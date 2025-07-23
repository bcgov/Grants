using Grants.ApplicantPortal.API.Core.Plugins;
using System.Text.Json;

namespace Grants.ApplicantPortal.API.Infrastructure.Plugins.Unity;

/// <summary>
/// Unity profile plugin for populating profile data from Unity systems
/// </summary>
public class UnityProfilePlugin(ILogger<UnityProfilePlugin> logger) : IProfilePlugin
{
    public string PluginId => "UNITY";

    public bool CanHandle(ProfilePopulationMetadata metadata)
    {
        return metadata.PluginId.Equals(PluginId, StringComparison.OrdinalIgnoreCase);
    }

    public async Task<ProfileData> PopulateProfileAsync(ProfilePopulationMetadata metadata, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Unity plugin populating profile for ProfileId: {ProfileId}", metadata.ProfileId);

        try
        {
            // Mock implementation - in real scenario, this would call Unity APIs
            await Task.Delay(100, cancellationToken); // Simulate API call delay

            var mockProfileData = new
            {
                ProfileId = metadata.ProfileId,
                Source = "Unity",
                PersonalInfo = new
                {
                    FirstName = "John",
                    LastName = "Doe",
                    Email = "john.doe@unity.gov",
                    Phone = "+1-555-0123"
                },
                Employment = new
                {
                    Department = "Department of Health",
                    Position = "Senior Analyst",
                    StartDate = "2020-01-15",
                    EmployeeId = "UNI-12345"
                },
                Security = new
                {
                    ClearanceLevel = "Secret",
                    BadgeNumber = "B789456",
                    LastUpdated = DateTime.UtcNow
                },
                Metadata = new
                {
                    PopulatedBy = PluginId,
                    PopulatedAt = DateTime.UtcNow,
                    Version = "1.0"
                }
            };

            var jsonData = JsonSerializer.Serialize(mockProfileData, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            });

            logger.LogInformation("Unity plugin successfully populated profile for ProfileId: {ProfileId}", metadata.ProfileId);

            return new ProfileData(
                metadata.ProfileId,
                metadata.PluginId,
                jsonData,
                DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unity plugin failed to populate profile for ProfileId: {ProfileId}", metadata.ProfileId);
            throw;
        }
    }
}
