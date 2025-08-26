using Grants.ApplicantPortal.API.Infrastructure.Data;
using Grants.ApplicantPortal.API.Infrastructure.Plugins;
using Grants.ApplicantPortal.API.Infrastructure.Plugins.Unity;
using Grants.ApplicantPortal.API.Infrastructure.Plugins.Demo;
using Grants.ApplicantPortal.API.Core.Plugins;
using Polly;
using Grants.ApplicantPortal.API.Core.Plugins.External;
using Grants.ApplicantPortal.API.Core.Plugins.PluginConfigurations.Interfaces;
using Grants.ApplicantPortal.API.Core.Features.Contributors.Interfaces;
using Grants.ApplicantPortal.API.Core.Features.Contributors.Services;

namespace Grants.ApplicantPortal.API.Infrastructure;

public static class InfrastructureServiceExtensions
{
  public static IServiceCollection AddInfrastructureServices(
    this IServiceCollection services,
    ConfigurationManager config,
    ILogger logger)
  {
    string? connectionString = config.GetConnectionString("DefaultConnection");
    Guard.Against.Null(connectionString);

    // Add DbContext with PostgreSQL provider
    services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));
    // Register repository support services
    services.AddRepositorySupport();
    // Register feature-specific services
    services.AddFeatureServices();
    // Register plugin support services
    services.AddPluginSupport();
    // Add Http Resilience policies
    services.AddHttpClient();

    logger.LogInformation("{Project} services registered", "Infrastructure");

    return services;
  }

  /// <summary>
  /// Add feature-specific services
  /// </summary>
  public static IServiceCollection AddFeatureServices(this IServiceCollection services)
  {
    // Register any additional feature-specific services here
    services.AddScoped<IDeleteContributorService, DeleteContributorService>();
    return services;
  }

  /// <summary>
  /// Adds repository support services including generic repositories and unit of work
  /// </summary>
  public static IServiceCollection AddRepositorySupport(this IServiceCollection services)
  {
    // Register any additional repository support services here
    services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));
    services.AddScoped(typeof(IReadRepository<>), typeof(EfRepository<>));
    return services;
  }

  /// <summary>
  /// Adds plugin support services including configuration, external service clients, and profile plugins
  /// </summary>
  public static IServiceCollection AddPluginSupport(this IServiceCollection services)
  {
    services.AddScoped<IPluginConfigurationService, PluginConfigurationService>();

    // Register profile plugins
    services.AddScoped<IProfilePlugin, UnityProfilePlugin>();
    services.AddScoped<IProfilePlugin, DemoProfilePlugin>();
    services.AddScoped<IProfilePluginFactory, ProfilePluginFactory>();

    return services;
  }

  public static IServiceCollection AddHttpClient(this IServiceCollection services)
  {
    // Configure HTTP client with resilience patterns for external service calls
    services.AddHttpClient<IExternalServiceClient, ExternalServiceClient>(client =>
    {
      client.Timeout = TimeSpan.FromSeconds(30);
      client.DefaultRequestHeaders.Add("User-Agent", "Grants-ApplicantPortal/1.0");
    })
    .AddStandardResilienceHandler(options =>
    {
      // Configure retry policy for transient failures
      options.Retry.MaxRetryAttempts = 3;
      options.Retry.BackoffType = DelayBackoffType.Exponential;
      options.Retry.Delay = TimeSpan.FromSeconds(1);
      options.Retry.MaxDelay = TimeSpan.FromSeconds(10);

      // Configure timeout - reduced to allow for proper circuit breaker sampling
      options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(10);

      // Configure circuit breaker - sampling duration must be at least double the timeout
      options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30); // 3x timeout for safety
      options.CircuitBreaker.FailureRatio = 0.5;
      options.CircuitBreaker.MinimumThroughput = 5;
      options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(30);

      // Handle specific HTTP status codes that should trigger retries
      options.Retry.ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
          .Handle<HttpRequestException>()
          .Handle<TaskCanceledException>()
          .HandleResult(response =>
              response.StatusCode == System.Net.HttpStatusCode.TooManyRequests ||
              response.StatusCode == System.Net.HttpStatusCode.InternalServerError ||
              response.StatusCode == System.Net.HttpStatusCode.BadGateway ||
              response.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable ||
              response.StatusCode == System.Net.HttpStatusCode.GatewayTimeout);
    });

    // Register external service client as scoped
    services.AddScoped<IExternalServiceClient, ExternalServiceClient>();

    return services;
  }
}
