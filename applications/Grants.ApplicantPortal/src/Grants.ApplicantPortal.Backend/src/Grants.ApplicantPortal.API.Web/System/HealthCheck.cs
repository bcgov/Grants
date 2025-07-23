using Grants.ApplicantPortal.API.Infrastructure.Plugins;

namespace Grants.ApplicantPortal.API.Web.System;

/// <summary>
/// System health check endpoint that includes plugin status
/// </summary>
public class HealthCheck : EndpointWithoutRequest<HealthCheckResponse>
{
    public override void Configure()
    {
        Get("/System/health");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "System health check";
            s.Description = "Returns system health status including plugin registry initialization";
            s.Responses[200] = "System is healthy";
            s.Responses[503] = "System is unhealthy";
        });
        
        Tags("System", "Health");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var isPluginRegistryInitialized = PluginRegistry.IsInitialized;
        var pluginCount = isPluginRegistryInitialized ? PluginRegistry.GetAllPluginIds().Count() : 0;
        
        var status = isPluginRegistryInitialized ? "Healthy" : "Unhealthy";
        var checks = new List<HealthCheckDto>
        {
            new("PluginRegistry", isPluginRegistryInitialized ? "Healthy" : "Unhealthy", 
                $"Initialized: {isPluginRegistryInitialized}, Plugins: {pluginCount}")
        };

        Response = new HealthCheckResponse(
            status,
            DateTime.UtcNow,
            checks);

        if (!isPluginRegistryInitialized)
        {
            await SendAsync(Response, 503, ct);
        }

        await Task.CompletedTask;
    }
}

/// <summary>
/// Health check response
/// </summary>
public record HealthCheckResponse(
    string Status,
    DateTime CheckedAt,
    IReadOnlyList<HealthCheckDto> Checks);

/// <summary>
/// Individual health check result
/// </summary>
public record HealthCheckDto(
    string Name,
    string Status,
    string Details);