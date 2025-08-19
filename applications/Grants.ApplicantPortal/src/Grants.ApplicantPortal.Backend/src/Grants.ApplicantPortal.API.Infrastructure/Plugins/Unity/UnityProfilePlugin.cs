using Grants.ApplicantPortal.API.Core.Plugins;
using System.Text.Json;

namespace Grants.ApplicantPortal.API.Infrastructure.Plugins.Unity;

/// <summary>
/// Unity profile plugin for populating profile data from Unity systems
/// </summary>
public class UnityProfilePlugin(ILogger<UnityProfilePlugin> logger) : IProfilePlugin
{
    public string PluginId => "UNITY";

    private static readonly IReadOnlyList<PluginSupportedFeature> SupportedFeatures = new List<PluginSupportedFeature>
    {
        new("GRANT1", "SUBMISSIONS", "Unity submissions profile data"),
        new("GRANT2", "ORGINFO", "Unity organization information"),
        new("GRANT3", "PAYMENTS", "Unity payments information")
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
        logger.LogInformation("Unity plugin populating profile for ProfileId: {ProfileId}", metadata.ProfileId);

        try
        {
            // Mock implementation - in real scenario, this would call Unity APIs
            await Task.Delay(100, cancellationToken); // Simulate API call delay

            var mockProfileData = GenerateMockData(metadata);

            var jsonData = JsonSerializer.Serialize(mockProfileData, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            });

            logger.LogInformation("Unity plugin successfully populated profile for ProfileId: {ProfileId}", metadata.ProfileId);

            return new ProfileData(
                metadata.ProfileId,
                metadata.PluginId,
                metadata.Provider,
                metadata.Key,
                jsonData);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unity plugin failed to populate profile for ProfileId: {ProfileId}", metadata.ProfileId);
            throw;
        }
    }

    private object GenerateMockData(ProfilePopulationMetadata metadata)
    {
        var baseData = new
        {
            ProfileId = metadata.ProfileId,
            Provider = metadata.Provider,
            Key = metadata.Key,
            Source = "Unity",
            PopulatedAt = DateTime.UtcNow,
            PopulatedBy = PluginId
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
