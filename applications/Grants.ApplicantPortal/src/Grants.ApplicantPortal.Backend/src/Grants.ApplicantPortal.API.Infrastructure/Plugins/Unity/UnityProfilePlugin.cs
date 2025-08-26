using Grants.ApplicantPortal.API.Core.Plugins;
using Grants.ApplicantPortal.API.Core.Plugins.External;
using System.Text.Json;

namespace Grants.ApplicantPortal.API.Infrastructure.Plugins.Unity;

/// <summary>
/// Unity profile plugin for populating profile data from Unity systems
/// </summary>
public class UnityProfilePlugin : IProfilePlugin
{
    private readonly ILogger<UnityProfilePlugin> _logger;
    private readonly IExternalServiceClient _externalServiceClient;

    public UnityProfilePlugin(
        ILogger<UnityProfilePlugin> logger,
        IExternalServiceClient externalServiceClient)
    {
        _logger = logger;
        _externalServiceClient = externalServiceClient;
    }

    public string PluginId => "UNITY";

    private static readonly IReadOnlyList<PluginSupportedFeature> SupportedFeatures = new List<PluginSupportedFeature>
    {
        new("UNITY", "PROFILE", "Unity user profile data"),
        new("UNITY", "EMPLOYMENT", "Unity employment information"),
        new("UNITY", "SECURITY", "Unity security clearance data")
    };

    public IReadOnlyList<PluginSupportedFeature> GetSupportedFeatures()
    {
        return SupportedFeatures;
    }

    public IReadOnlyList<string> GetSupportedProviders()
    {
        return SupportedFeatures
            .Select(f => f.Provider)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public IReadOnlyList<string> GetSupportedKeys(string provider)
    {
        if (string.IsNullOrWhiteSpace(provider))
            return new List<string>();

        return SupportedFeatures
            .Where(f => f.Provider.Equals(provider, StringComparison.OrdinalIgnoreCase))
            .Select(f => f.Key)
            .ToList();
    }

    public bool CanHandle(ProfilePopulationMetadata metadata)
    {
        if (!metadata.PluginId.Equals(PluginId, StringComparison.OrdinalIgnoreCase))
            return false;

        // Check if the provider/key combination is supported
        return SupportedFeatures.Any(f => 
            f.Provider.Equals(metadata.Provider, StringComparison.OrdinalIgnoreCase) &&
            f.Key.Equals(metadata.Key, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<ProfileData> PopulateProfileAsync(ProfilePopulationMetadata metadata, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Unity plugin populating profile for ProfileId: {ProfileId}", metadata.ProfileId);

        try
        {
            // Use the external service client to get data from Unity APIs
            var response = await CallUnityServiceAsync(metadata, cancellationToken);

            if (!response.IsSuccess)
            {
                _logger.LogError("Unity service call failed for ProfileId: {ProfileId}. Error: {Error}", 
                    metadata.ProfileId, response.ErrorMessage);
                
                // Fall back to mock data if external service fails
                _logger.LogWarning("Falling back to mock data for ProfileId: {ProfileId}", metadata.ProfileId);
                var mockData = GenerateMockData(metadata);
                var mockJsonData = JsonSerializer.Serialize(mockData, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = true
                });

                return new ProfileData(
                    metadata.ProfileId,
                    metadata.PluginId,
                    metadata.Provider,
                    metadata.Key,
                    mockJsonData);
            }

            _logger.LogInformation("Unity plugin successfully populated profile for ProfileId: {ProfileId}", metadata.ProfileId);

            return new ProfileData(
                metadata.ProfileId,
                metadata.PluginId,
                metadata.Provider,
                metadata.Key,
                response.Data!);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unity plugin failed to populate profile for ProfileId: {ProfileId}", metadata.ProfileId);
            throw;
        }
    }

    private async Task<ExternalServiceResponse<string>> CallUnityServiceAsync(
        ProfilePopulationMetadata metadata, 
        CancellationToken cancellationToken)
    {
        var endpoint = BuildEndpoint(metadata.Provider, metadata.Key, metadata.ProfileId);
        
        var request = new ExternalServiceRequest
        {
            Endpoint = endpoint,
            Method = HttpMethod.Get,
            QueryParameters = new Dictionary<string, string>
            {
                ["profileId"] = metadata.ProfileId.ToString(),
                ["provider"] = metadata.Provider,
                ["key"] = metadata.Key
            }
        };

        // Add any additional data as query parameters
        if (metadata.AdditionalData?.Any() == true)
        {
            foreach (var kvp in metadata.AdditionalData)
            {
                request.QueryParameters[kvp.Key] = kvp.Value?.ToString() ?? string.Empty;
            }
        }

        return await _externalServiceClient.CallAsync(PluginId, request, cancellationToken);
    }

    private static string BuildEndpoint(string provider, string key, Guid profileId)
    {
        return (provider?.ToUpper(), key?.ToUpper()) switch
        {
            ("UNITY", "PROFILE") => $"/api/v1/profiles/{profileId}",
            ("UNITY", "EMPLOYMENT") => $"/api/v1/profiles/{profileId}/employment",
            ("UNITY", "SECURITY") => $"/api/v1/profiles/{profileId}/security",
            _ => $"/api/v1/profiles/{profileId}/data"
        };
    }

    private object GenerateMockData(ProfilePopulationMetadata metadata)
    {
        var baseData = new
        {
            ProfileId = metadata.ProfileId,
            Provider = metadata.Provider,
            Key = metadata.Key,
            Source = "Unity (Mock)",
            PopulatedAt = DateTime.UtcNow,
            PopulatedBy = PluginId,
            IsMockData = true
        };

        return (metadata.Provider?.ToUpper(), metadata.Key?.ToUpper()) switch
        {
            ("UNITY", "PROFILE") => GenerateProfileData(baseData),
            ("UNITY", "EMPLOYMENT") => GenerateEmploymentData(baseData),
            ("UNITY", "SECURITY") => GenerateSecurityData(baseData),
            _ => GenerateDefaultData(baseData)
        };
    }

    private object GenerateProfileData(object baseData)
    {
        return new
        {
            baseData,
            Data = new
            {
                PersonalInfo = new
                {
                    FirstName = "John",
                    LastName = "Doe",
                    Email = "john.doe@unity.gov",
                    Phone = "+1-555-0123",
                    EmployeeId = "UNI-12345"
                }
            }
        };
    }

    private object GenerateEmploymentData(object baseData)
    {
        return new
        {
            baseData,
            Data = new
            {
                Employment = new
                {
                    Department = "Department of Health",
                    Position = "Senior Analyst",
                    StartDate = "2020-01-15",
                    EmployeeId = "UNI-12345",
                    Manager = "Jane Smith",
                    Location = "Building A, Room 205"
                }
            }
        };
    }

    private object GenerateSecurityData(object baseData)
    {
        return new
        {
            baseData,
            Data = new
            {
                Security = new
                {
                    ClearanceLevel = "Secret",
                    BadgeNumber = "B789456",
                    LastUpdated = DateTime.UtcNow,
                    ExpiryDate = DateTime.UtcNow.AddYears(2),
                    AccessLevel = "Level 3"
                }
            }
        };
    }

    private object GenerateDefaultData(object baseData)
    {
        return new
        {
            baseData,
            Data = new
            {
                Message = "Unity data available for:",
                AvailableProviders = new[] { "Unity" },
                AvailableKeys = new[] { "Profile", "Employment", "Security" },
                Instructions = "Use Provider and Key parameters to get specific Unity data",
                Examples = new[]
                {
                    "Provider=Unity, Key=Profile - Get Unity user profile data",
                    "Provider=Unity, Key=Employment - Get Unity employment information",
                    "Provider=Unity, Key=Security - Get Unity security clearance data"
                }
            }
        };
    }
}
