using Grants.ApplicantPortal.API.Infrastructure.Messaging.Abstractions;
using Grants.ApplicantPortal.API.Infrastructure.Messaging.BackgroundJobs;
using Grants.ApplicantPortal.API.Infrastructure.Messaging.Configuration;
using Grants.ApplicantPortal.API.Infrastructure.Messaging.Handlers;
using Grants.ApplicantPortal.API.Infrastructure.Messaging.Inbox;
using Grants.ApplicantPortal.API.Infrastructure.Messaging.Jobs;
using Grants.ApplicantPortal.API.Infrastructure.Messaging.Messages;
using Grants.ApplicantPortal.API.Infrastructure.Messaging.Outbox;
using Grants.ApplicantPortal.API.Infrastructure.Messaging.RabbitMQ;
using Grants.ApplicantPortal.API.Infrastructure.Messaging.Services;
using Microsoft.Extensions.Caching.Distributed;
using Quartz;
using StackExchange.Redis;

namespace Grants.ApplicantPortal.API.Infrastructure.Messaging;

/// <summary>
/// Extension methods for registering messaging services
/// </summary>
public static class MessagingServiceExtensions
{
    /// <summary>
    /// Adds all messaging services including Inbox/Outbox pattern, background jobs, and distributed locking
    /// </summary>
    public static IServiceCollection AddMessagingServices(
        this IServiceCollection services, 
        IConfiguration configuration,
        ILogger logger)
    {
        // Register configuration
        services.Configure<MessagingOptions>(configuration.GetSection(MessagingOptions.SectionName));

        // Register core messaging services
        services.AddScoped<IMessagePublisher, OutboxMessagePublisher>();
        services.AddScoped<IOutboxRepository, OutboxRepository>();
        services.AddScoped<IInboxRepository, InboxRepository>();

        // Register distributed locking (requires Redis connection)
        services.AddDistributedLocking(configuration, logger);

        // Register background jobs
        services.AddMessagingBackgroundJobs(configuration, logger);

        // Register RabbitMQ services
        services.AddRabbitMQServices(configuration, logger);

        // Register message handlers
        services.AddMessageHandlers(logger);

        logger.LogInformation("Messaging services registered successfully");

        return services;
    }

    /// <summary>
    /// Adds distributed locking - Redis if configured, otherwise in-memory
    /// </summary>
    private static IServiceCollection AddDistributedLocking(
        this IServiceCollection services,
        IConfiguration configuration,
        ILogger logger)
    {
        // Check if Redis connection is configured in appsettings
        var redisConnectionString = configuration.GetConnectionString("Redis");
        var hasRedisConfig = !string.IsNullOrEmpty(redisConnectionString);
        
        if (hasRedisConfig)
        {
            logger.LogInformation("Redis connection string found. Using Redis-based distributed locking");
            
            try
            {
                // Check if IConnectionMultiplexer is already registered (by caching configuration)
                var hasRedisConnection = services.Any(x => x.ServiceType == typeof(IConnectionMultiplexer));
                
                if (!hasRedisConnection)
                {
                    // Register Redis connection for messaging (development scenario)
                    services.AddSingleton<IConnectionMultiplexer>(provider =>
                    {
                        var multiplexer = ConnectionMultiplexer.Connect(redisConnectionString);
                        logger.LogDebug("Redis connection established for messaging services");
                        return multiplexer;
                    });
                    
                    logger.LogDebug("Registered IConnectionMultiplexer for messaging services");
                }
                
                // Register our Redis-based distributed lock service
                services.AddSingleton<IDistributedLock, RedisDistributedLock>();
                
                logger.LogInformation("Redis distributed locking registered successfully");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to register Redis distributed locking. Falling back to in-memory implementation");
                
                // Fallback to in-memory if Redis fails
                if (!services.Any(x => x.ServiceType == typeof(IDistributedCache)))
                {
                    services.AddDistributedMemoryCache();
                }
                services.AddSingleton<IDistributedLock, InMemoryDistributedLock>();
                
                logger.LogWarning("Using in-memory distributed locking due to Redis registration failure");
            }
        }
        else
        {
            logger.LogInformation("No Redis connection string found. Using in-memory distributed locking");
            
            // Register in-memory distributed cache if not already registered
            if (!services.Any(x => x.ServiceType == typeof(IDistributedCache)))
            {
                services.AddDistributedMemoryCache();
                logger.LogDebug("Registered IDistributedMemoryCache for in-memory distributed locking");
            }
            
            // Register our in-memory distributed lock implementation
            services.AddSingleton<IDistributedLock, InMemoryDistributedLock>();
            
            logger.LogInformation("In-memory distributed locking registered successfully");
        }

        return services;
    }

    /// <summary>
    /// Adds Quartz.NET background jobs for processing messages
    /// </summary>
    private static IServiceCollection AddMessagingBackgroundJobs(
        this IServiceCollection services,
        IConfiguration configuration,
        ILogger logger)
    {
        var messagingOptions = configuration.GetSection(MessagingOptions.SectionName).Get<MessagingOptions>() 
                              ?? new MessagingOptions();

        if (!messagingOptions.BackgroundJobs.Enabled)
        {
            logger.LogInformation("Background jobs are disabled in configuration");
            return services;
        }

        // Add Quartz.NET
        services.AddQuartz(q =>
        {                        
            // Configure Quartz options
            q.UseSimpleTypeLoader();
            q.UseInMemoryStore();
            q.UseDefaultThreadPool(tp =>
            {
                tp.MaxConcurrency = messagingOptions.BackgroundJobs.MaxConcurrency;
            });

            // Configure the Outbox processor job
            var outboxJobKey = new JobKey("OutboxProcessorJob");
            q.AddJob<OutboxProcessorJob>(opts => opts.WithIdentity(outboxJobKey));
            
            q.AddTrigger(opts => opts
                .ForJob(outboxJobKey)
                .WithIdentity("OutboxProcessorJob-trigger")
                .WithSimpleSchedule(x => x
                    .WithIntervalInSeconds(messagingOptions.Outbox.PollingIntervalSeconds)
                    .RepeatForever())
                .StartNow());

            // Configure the Inbox processor job
            var inboxJobKey = new JobKey("InboxProcessorJob");
            q.AddJob<InboxProcessorJob>(opts => opts.WithIdentity(inboxJobKey));
            
            q.AddTrigger(opts => opts
                .ForJob(inboxJobKey)
                .WithIdentity("InboxProcessorJob-trigger")
                .WithSimpleSchedule(x => x
                    .WithIntervalInSeconds(messagingOptions.Inbox.PollingIntervalSeconds)
                    .RepeatForever())
                .StartNow());

            // TODO: Add cleanup jobs for old messages

            logger.LogInformation("Quartz.NET background jobs configured with {MaxConcurrency} max concurrency", 
                messagingOptions.BackgroundJobs.MaxConcurrency);
        });

        services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);

        logger.LogInformation("Messaging background jobs registered successfully");

        return services;
    }

    /// <summary>
    /// Adds RabbitMQ message publishing services
    /// </summary>
    private static IServiceCollection AddRabbitMQServices(
        this IServiceCollection services,
        IConfiguration configuration,
        ILogger logger)
    {
        var messagingOptions = configuration.GetSection(MessagingOptions.SectionName).Get<MessagingOptions>() 
                              ?? new MessagingOptions();

        try
        {
            // Register RabbitMQ configuration
            var rabbitMQConfig = new RabbitMQConfiguration
            {
                HostName = messagingOptions.RabbitMQ.HostName,
                Port = messagingOptions.RabbitMQ.Port,
                UserName = messagingOptions.RabbitMQ.UserName,
                Password = messagingOptions.RabbitMQ.Password,
                VirtualHost = messagingOptions.RabbitMQ.VirtualHost,
                UseSsl = messagingOptions.RabbitMQ.UseSsl,
                ConnectionTimeoutSeconds = (int)messagingOptions.RabbitMQ.ConnectionTimeout.TotalSeconds,
                RetryCount = messagingOptions.RabbitMQ.RetryCount,
                RetryDelay = messagingOptions.RabbitMQ.RetryDelay
            };

            services.AddSingleton(rabbitMQConfig);

            // Register RabbitMQ publisher
            services.AddSingleton<IRabbitMQPublisher, RabbitMQPublisher>();

            // Register RabbitMQ consumer as scoped since it depends on scoped services
            services.AddScoped<RabbitMQConsumer>();

            // Register RabbitMQ consumer hosted service
            services.AddHostedService<RabbitMQConsumerHostedService>();

            logger.LogInformation("RabbitMQ services registered successfully - Host: {Host}:{Port}", 
                rabbitMQConfig.HostName, rabbitMQConfig.Port);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to register RabbitMQ services - outbox processing will use simulation mode");
            
            // Don't register IRabbitMQPublisher if RabbitMQ configuration fails
            // OutboxProcessorJob will detect this and use simulation mode
        }

        return services;
    }

    /// <summary>
    /// Adds message handlers and handler resolution
    /// </summary>
    private static IServiceCollection AddMessageHandlers(
        this IServiceCollection services,
        ILogger logger)
    {
        // Register message handler resolver
        services.AddSingleton<IMessageHandlerResolver, ServiceProviderMessageHandlerResolver>();

        // Register plugin message router as scoped since it depends on scoped services
        services.AddScoped<IPluginMessageRouter, PluginMessageRouter>();

        // Register domain message handlers
        services.AddScoped<IMessageHandler<ProfileUpdatedMessage>, ProfileUpdatedMessageHandler>();
        services.AddScoped<IMessageHandler<ContactCreatedMessage>, ContactCreatedMessageHandler>();
        services.AddScoped<IMessageHandler<SystemEventMessage>, SystemEventMessageHandler>();

        // Register plugin-specific message handlers
        services.AddScoped<IPluginMessageHandler, DemoPluginMessageHandler>();

        // TODO: Add more plugin handlers as needed
        // services.AddScoped<IPluginMessageHandler, UnityPluginMessageHandler>();

        // TODO: Add more domain message handlers as needed
        // services.AddScoped<IMessageHandler<AddressUpdatedMessage>, AddressUpdatedMessageHandler>();
        // services.AddScoped<IMessageHandler<OrganizationUpdatedMessage>, OrganizationUpdatedMessageHandler>();

        logger.LogInformation("Message handlers registered successfully");

        return services;
    }
}
