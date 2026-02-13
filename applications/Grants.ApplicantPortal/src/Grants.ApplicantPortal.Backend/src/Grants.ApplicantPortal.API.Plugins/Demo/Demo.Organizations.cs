using Ardalis.Result;
using Grants.ApplicantPortal.API.Core.DTOs;
using Grants.ApplicantPortal.API.Core.Plugins;
using Grants.ApplicantPortal.API.Plugins.Demo.Data;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace Grants.ApplicantPortal.API.Plugins.Demo;

/// <summary>
/// Organization management implementation for Demo plugin
/// </summary>
public partial class DemoPlugin
{
    public async Task<Result> EditOrganizationAsync(
        EditOrganizationRequest editRequest,
        ProfileContext profileContext,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Demo plugin editing organization {OrganizationId} for ProfileId: {ProfileId}",
            editRequest.OrganizationId, profileContext.ProfileId);

        try
        {
            // Simulate some processing time
            await Task.Delay(85, cancellationToken);

            // Update the organization in our in-memory store
            var success = OrganizationsData.UpdateOrganization(profileContext.Provider, profileContext.ProfileId, editRequest.OrganizationId, editRequest);
            
            if (!success)
            {
                _logger.LogWarning("Organization {OrganizationId} not found for ProfileId: {ProfileId}",
                    editRequest.OrganizationId, profileContext.ProfileId);
                return Result.NotFound();
            }

            // PERSIST TO REDIS: Update the cached organization data
            await PersistOrganizationDataToRedis(profileContext.Provider, profileContext.ProfileId, cancellationToken);

            // Log the organization edit details
            _logger.LogInformation("Demo plugin edited organization - ID: {OrganizationId}, Name: {Name}, Type: {Type}, Status: {Status}",
                editRequest.OrganizationId, editRequest.Name, editRequest.OrganizationType, editRequest.Status);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Demo plugin failed to edit organization {OrganizationId} for ProfileId: {ProfileId}",
                editRequest.OrganizationId, profileContext.ProfileId);
            return Result.Error("Failed to edit organization in demo system");
        }
    }

    /// <summary>
    /// Persists the current organization data to Redis cache
    /// </summary>
    private async Task PersistOrganizationDataToRedis(string provider, Guid profileId, CancellationToken cancellationToken)
    {
        try
        {
            // Generate the current organization data using the existing OrganizationsData logic
            var metadata = new ProfilePopulationMetadata(
                profileId,
                PluginId,
                provider,
                "ORGINFO");

            var mockData = GenerateSeedingMockData(metadata);
            var jsonData = JsonSerializer.Serialize(mockData, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            });

            var profileData = new ProfileData(
                profileId,
                PluginId,
                provider,
                "ORGINFO",
                jsonData);

            // Store updated data in Redis
            var cacheKey = $"{_cacheOptions.Value.CacheKeyPrefix}{profileId}:DEMO:{provider}:ORGINFO";
            var profileDataBytes = JsonSerializer.SerializeToUtf8Bytes(profileData);
            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(365)
            };

            await _distributedCache.SetAsync(cacheKey, profileDataBytes, cacheOptions, cancellationToken);
            
            _logger.LogDebug("Persisted organization data to Redis for ProfileId: {ProfileId}, Provider: {Provider}", 
                profileId, provider);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to persist organization data to Redis for ProfileId: {ProfileId}, Provider: {Provider}", 
                profileId, provider);
            throw; // This is critical - if we can't persist, the operation should fail
        }
    }
}
