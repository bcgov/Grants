using System.Reflection;
using Microsoft.Extensions.Options;
using Grants.ApplicantPortal.API.Web.Configurations;

namespace Grants.ApplicantPortal.API.Web.System;

/// <summary>
/// System information endpoint
/// Provides version and environment information
/// </summary>
public class SystemInfo : EndpointWithoutRequest<SystemInfoResponse>
{
    private readonly IOptions<KeycloakConfiguration> _keycloakOptions;

    public SystemInfo(IOptions<KeycloakConfiguration> keycloakOptions)
    {
        _keycloakOptions = keycloakOptions;
    }

    public override void Configure()
    {
        Get("/System/info");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "System information";
            s.Description = "Returns system version, environment, and configuration details";
            s.Responses[200] = "System information retrieved";
        });
        
        Tags("System", "Information");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version?.ToString() ?? "Unknown";
        var buildDate = GetBuildDate(assembly);
        
        // Check Keycloak configuration for debugging
        var keycloakConfig = _keycloakOptions.Value;
        var keycloakStatus = "Not configured";
        if (keycloakConfig != null)
        {
            keycloakStatus = $"Configured - Server: {(!string.IsNullOrEmpty(keycloakConfig.AuthServerUrl) ? "✓" : "✗")}, " +
                           $"Realm: {(!string.IsNullOrEmpty(keycloakConfig.Realm) ? "✓" : "✗")}, " +
                           $"Resource: {(!string.IsNullOrEmpty(keycloakConfig.Resource) ? "✓" : "✗")}, " +
                           $"Secret: {(!string.IsNullOrEmpty(keycloakConfig.Credentials?.Secret) ? "✓" : "✗")}";
        }
        else
        {
            keycloakStatus = "Configuration object is null";
        }
        
        Response = new SystemInfoResponse(
            "Grants Applicant Portal API",
            version,
            Environment.MachineName,
            Environment.OSVersion.ToString(),
            Environment.Version.ToString(),
            buildDate,
            keycloakStatus,
            DateTime.UtcNow);

        await Task.CompletedTask;
    }

    private static DateTime GetBuildDate(Assembly assembly)
    {
        // Try to get build date from assembly attributes or file info
        var location = assembly.Location;
        if (!string.IsNullOrEmpty(location) && File.Exists(location))
        {
            return File.GetLastWriteTime(location);
        }
        
        return DateTime.UtcNow; // Fallback
    }
}

/// <summary>
/// System information response
/// </summary>
public record SystemInfoResponse(
    string ApplicationName,
    string Version,
    string MachineName,
    string OperatingSystem,
    string RuntimeVersion,
    DateTime BuildDate,
    string KeycloakConfigStatus,
    DateTime RequestedAt);
