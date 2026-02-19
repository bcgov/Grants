using Ardalis.Result;
using Grants.ApplicantPortal.API.Core.DTOs;
using Grants.ApplicantPortal.API.Core.Plugins;
using Grants.ApplicantPortal.API.Plugins.Demo.Data;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace Grants.ApplicantPortal.API.Plugins.Demo;

/// <summary>
/// Address management implementation for Demo plugin
/// </summary>
public partial class DemoPlugin
{
    public async Task<Result> EditAddressAsync(
        EditAddressRequest editRequest,
        ProfileContext profileContext,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Demo plugin editing address {AddressId} for ProfileId: {ProfileId}",
            editRequest.AddressId, profileContext.ProfileId);

        try
        {
            // Simulate some processing time
            await Task.Delay(75, cancellationToken);

            // Update the address in our in-memory store
            var success = AddressesData.UpdateAddress(profileContext.Provider, profileContext.ProfileId, editRequest.AddressId, editRequest);
            
            if (!success)
            {
                logger.LogWarning("Address {AddressId} not found for ProfileId: {ProfileId}",
                    editRequest.AddressId, profileContext.ProfileId);
                return Result.NotFound();
            }

            // PERSIST TO REDIS: Update the cached addresses data
            await PersistAddressesDataToRedis(profileContext.Provider, profileContext.ProfileId, cancellationToken);

            // Log the address edit details
            logger.LogInformation("Demo plugin edited address - ID: {AddressId}, Type: {Type}, Address: {Address}, City: {City}",
                editRequest.AddressId, editRequest.Type, editRequest.Address, editRequest.City);

            return Result.Success();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Demo plugin failed to edit address {AddressId} for ProfileId: {ProfileId}",
                editRequest.AddressId, profileContext.ProfileId);
            return Result.Error("Failed to edit address in demo system");
        }
    }

    public async Task<Result> SetAsPrimaryAddressAsync(
        Guid addressId,
        ProfileContext profileContext,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Demo plugin setting address {AddressId} as primary for ProfileId: {ProfileId}",
            addressId, profileContext.ProfileId);

        try
        {
            // Simulate some processing time
            await Task.Delay(55, cancellationToken);

            // Set the address as primary in our in-memory store
            var success = AddressesData.SetAddressAsPrimary(profileContext.Provider, profileContext.ProfileId, addressId);
            
            if (!success)
            {
                logger.LogWarning("Address {AddressId} not found for ProfileId: {ProfileId}",
                    addressId, profileContext.ProfileId);
                return Result.NotFound();
            }

            // PERSIST TO REDIS: Update the cached addresses data
            await PersistAddressesDataToRedis(profileContext.Provider, profileContext.ProfileId, cancellationToken);

            // Log the address set as primary operation
            logger.LogInformation("Demo plugin set address {AddressId} as primary for ProfileId: {ProfileId}",
                addressId, profileContext.ProfileId);

            return Result.Success();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Demo plugin failed to set address {AddressId} as primary for ProfileId: {ProfileId}",
                addressId, profileContext.ProfileId);
            return Result.Error("Failed to set address as primary in demo system");
        }
    }

    /// <summary>
    /// Persists the current addresses data to Redis cache
    /// </summary>
    private async Task PersistAddressesDataToRedis(string provider, Guid profileId, CancellationToken cancellationToken)
    {
        try
        {
            // Generate the current addresses data using the existing AddressesData logic
            var metadata = new ProfilePopulationMetadata(
                profileId,
                PluginId,
                provider,
                "ADDRESSINFO");

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
                "ADDRESSINFO",
                jsonData);

            // Store updated data in Redis
            var cacheKey = $"{_cacheOptions.Value.CacheKeyPrefix}{profileId}:DEMO:{provider}:ADDRESSES";
            var profileDataBytes = JsonSerializer.SerializeToUtf8Bytes(profileData);
            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(365)
            };

            await distributedCache.SetAsync(cacheKey, profileDataBytes, cacheOptions, cancellationToken);
            
            logger.LogDebug("Persisted addresses data to Redis for ProfileId: {ProfileId}, Provider: {Provider}", 
                profileId, provider);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to persist addresses data to Redis for ProfileId: {ProfileId}, Provider: {Provider}", 
                profileId, provider);
            throw; // This is critical - if we can't persist, the operation should fail
        }
    }
}
