using Grants.ApplicantPortal.API.Core.Plugins;
using System.Text.Json;

namespace Grants.ApplicantPortal.API.Infrastructure.Plugins.Demo;

/// <summary>
/// Demo profile plugin for testing and demonstration purposes
/// </summary>
public class DemoProfilePlugin(ILogger<DemoProfilePlugin> logger) : IProfilePlugin
{
    public string PluginId => "DEMO";

    public bool CanHandle(ProfilePopulationMetadata metadata)
    {
        return metadata.PluginId.Equals(PluginId, StringComparison.OrdinalIgnoreCase);
    }

    public async Task<ProfileData> PopulateProfileAsync(ProfilePopulationMetadata metadata, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Demo plugin populating profile for ProfileId: {ProfileId}", metadata.ProfileId);

        try
        {
            // Simulate some processing time
            await Task.Delay(50, cancellationToken);

            var mockProfileData = new
            {
                ProfileId = metadata.ProfileId,
                Source = "Demo System",
                PersonalInfo = new
                {
                    FirstName = "Demo",
                    LastName = "User",
                    Email = "demo.user@example.com",
                    Phone = "+1-555-DEMO"
                },
                DemoData = new
                {
                    Type = "Sample",
                    Version = "2.0",
                    Features = new[] { "Fast", "Reliable", "Secure" },
                    LastUpdated = DateTime.UtcNow
                },
                Metadata = new
                {
                    PopulatedBy = PluginId,
                    PopulatedAt = DateTime.UtcNow,
                    AdditionalData = metadata.AdditionalData
                }
            };

            var jsonData = JsonSerializer.Serialize(mockProfileData, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            });

            logger.LogInformation("Demo plugin successfully populated profile for ProfileId: {ProfileId}", metadata.ProfileId);

            return new ProfileData(
                metadata.ProfileId,
                metadata.PluginId,
                jsonData,
                DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Demo plugin failed to populate profile for ProfileId: {ProfileId}", metadata.ProfileId);
            throw;
        }
    }
}