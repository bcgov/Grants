using Grants.ApplicantPortal.API.Core.Plugins;
using Grants.ApplicantPortal.API.Core.Plugins.External;
using Grants.ApplicantPortal.API.Core.Plugins.PluginConfigurations.Interfaces;
using System.Text.Json;

namespace Grants.ApplicantPortal.API.Web.System;

/// <summary>
/// Manages plugin configurations
/// </summary>
public class ManagePluginConfiguration : EndpointWithoutRequest
{
    public override void Configure()
    {
        Post("/System/plugins/{pluginId}/configuration");
        AllowAnonymous(); // For demo purposes - should be restricted in production
        Summary(s =>
        {
            s.Summary = "Create or update plugin configuration";
            s.Description = "Creates or updates the configuration for a specific plugin";
            s.Responses[200] = "Configuration saved successfully";
            s.Responses[400] = "Invalid configuration data";
        });
        
        Tags("System", "Plugins");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var pluginId = Route<string>("pluginId")!;
        var configurationService = Resolve<IPluginConfigurationService>();
        
        // Read the JSON configuration from the request body
        using var reader = new StreamReader(HttpContext.Request.Body);
        var requestBody = await reader.ReadToEndAsync();
        
        if (string.IsNullOrWhiteSpace(requestBody))
        {
            await SendErrorsAsync(400, ct);
            return;
        }

        try
        {
            // Validate that it's valid JSON
            JsonDocument.Parse(requestBody);
            
            // Save the configuration
            await configurationService.CreateOrUpdateConfigurationAsync(
                pluginId, 
                requestBody, 
                "System", // In production, this would be the authenticated user
                ct);

            await SendOkAsync(new { message = $"Configuration for plugin {pluginId} saved successfully" }, ct);
        }
        catch (JsonException)
        {
            await SendErrorsAsync(400, ct);
        }
        catch (Exception ex)
        {
            await SendErrorsAsync(500, ct);
        }
    }
}

/// <summary>
/// Gets plugin configuration
/// </summary>
public class GetPluginConfiguration : EndpointWithoutRequest<PluginConfigurationResponse>
{
    public override void Configure()
    {
        Get("/System/plugins/{pluginId}/configuration");
        AllowAnonymous(); // For demo purposes - should be restricted in production
        Summary(s =>
        {
            s.Summary = "Get plugin configuration";
            s.Description = "Retrieves the configuration for a specific plugin";
            s.Responses[200] = "Configuration retrieved successfully";
            s.Responses[404] = "Configuration not found";
        });
        
        Tags("System", "Plugins");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var pluginId = Route<string>("pluginId")!;
        var configurationService = Resolve<IPluginConfigurationService>();
        
        var configuration = await configurationService.GetConfigurationAsync(pluginId, ct);
        
        if (configuration == null)
        {
            await SendNotFoundAsync(ct);
            return;
        }

        Response = new PluginConfigurationResponse(
            configuration.PluginId,
            configuration.ConfigurationJson,
            configuration.CreatedAt,
            configuration.UpdatedAt,
            configuration.IsActive);
    }
}

/// <summary>
/// Response model for plugin configuration
/// </summary>
public record PluginConfigurationResponse(
    string PluginId,
    string ConfigurationJson,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    bool IsActive);

/// <summary>
/// Creates sample plugin configuration for Unity
/// </summary>
public class CreateSampleUnityConfiguration : EndpointWithoutRequest
{
    public override void Configure()
    {
        Post("/System/plugins/unity/sample-config");
        AllowAnonymous(); // For demo purposes
        Summary(s =>
        {
            s.Summary = "Create sample Unity plugin configuration";
            s.Description = "Creates a sample configuration for the Unity plugin for testing purposes";
            s.Responses[200] = "Sample configuration created";
        });
        
        Tags("System", "Plugins");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var configurationService = Resolve<IPluginConfigurationService>();
        
        var sampleConfig = new ExternalServiceConfiguration
        {
            BaseUrl = "https://api.unity.example.gov",
            ApiKey = "sample-api-key-replace-with-real-key",
            Headers = new Dictionary<string, string>
            {
                ["Content-Type"] = "application/json",
                ["Accept"] = "application/json"
            },
            TimeoutSeconds = 30,
            MaxRetryAttempts = 3,
            EnableCircuitBreaker = true
        };

        var configJson = JsonSerializer.Serialize(sampleConfig, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        });

        await configurationService.CreateOrUpdateConfigurationAsync(
            "UNITY", 
            configJson, 
            "System",
            ct);

        await SendOkAsync(new { 
            message = "Sample Unity configuration created",
            configuration = sampleConfig
        }, ct);
    }
}
