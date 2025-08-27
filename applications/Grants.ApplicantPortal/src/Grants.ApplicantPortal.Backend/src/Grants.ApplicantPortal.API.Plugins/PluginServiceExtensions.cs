using Grants.ApplicantPortal.API.Core;
using Grants.ApplicantPortal.API.Core.Features.PluginConfigurations.Services;
using Grants.ApplicantPortal.API.Core.Plugins;
using Grants.ApplicantPortal.API.Plugins.Demo;
using Grants.ApplicantPortal.API.Plugins.Unity;
using Microsoft.Extensions.DependencyInjection;
using Polly;

namespace Grants.ApplicantPortal.API.Plugins;

public static class PluginServiceExtensions
{
  public static IServiceCollection AddPluginServices(
   this IServiceCollection services,
   ILogger logger)
  {

    // Register plugin support services
    services.AddPluginSupport();
    // Add Http Resilience policies
    services.AddResilientHttpClientSupport();

    logger.LogInformation("{Project} services registered", "Plugins");

    return services;
  }

  /// <summary>
  /// Adds plugin support services including configuration, external service clients, and profile plugins
  /// </summary>
  internal static IServiceCollection AddPluginSupport(this IServiceCollection services)
  {
    services.AddScoped<PluginConfigurationService, PluginConfigurationService>();

    // Register profile plugins
    services.AddScoped<IProfilePlugin, UnityProfilePlugin>();
    services.AddScoped<IProfilePlugin, DemoProfilePlugin>();
    services.AddScoped<IProfilePluginFactory, ProfilePluginFactory>();

    return services;
  }

  /// <summary>
  /// Add httpclient with resilience policies for external service calls
  /// </summary>
  /// <param name="services"></param>
  /// <returns></returns>
  internal static IServiceCollection AddResilientHttpClientSupport(this IServiceCollection services)
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
