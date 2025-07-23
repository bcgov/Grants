using Grants.ApplicantPortal.API.Core.Plugins;

namespace Grants.ApplicantPortal.API.UseCases.Profiles.Retrieve;

/// <summary>
/// Query handler for retrieving cached profile data for a specific plugin
/// </summary>
public class RetrieveProfileQueryHandler(
    IDistributedCache distributedCache,
    ILogger<RetrieveProfileQueryHandler> logger)
    : IQueryHandler<RetrieveProfileQuery, Result<ProfileData>>
{
    private const string CACHE_KEY_PREFIX = "profile:";
    
    public async Task<Result<ProfileData>> Handle(RetrieveProfileQuery request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Retrieving cached profile data for ProfileId: {ProfileId}, PluginId: {PluginId}", 
            request.ProfileId, request.PluginId);

        try
        {
            // Use ProfileId and PluginId in the cache key
            var cacheKey = $"{CACHE_KEY_PREFIX}{request.ProfileId}:{request.PluginId}";
            var cachedData = await distributedCache.GetStringAsync(cacheKey, cancellationToken);

            if (string.IsNullOrEmpty(cachedData))
            {
                logger.LogInformation("No cached data found for ProfileId: {ProfileId}, PluginId: {PluginId}", 
                    request.ProfileId, request.PluginId);
                return Result.NotFound($"No cached profile data found for ProfileId: {request.ProfileId}, PluginId: {request.PluginId}");
            }

            var profileData = JsonSerializer.Deserialize<ProfileData>(cachedData);
            if (profileData == null)
            {
                logger.LogWarning("Failed to deserialize cached data for ProfileId: {ProfileId}, PluginId: {PluginId}", 
                    request.ProfileId, request.PluginId);
                return Result.Error("Failed to deserialize cached profile data");
            }

            logger.LogInformation("Successfully retrieved cached profile data for ProfileId: {ProfileId}, PluginId: {PluginId}", 
                request.ProfileId, request.PluginId);
            return Result.Success(profileData);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving cached profile data for ProfileId: {ProfileId}, PluginId: {PluginId}", 
                request.ProfileId, request.PluginId);
            return Result.Error($"Failed to retrieve cached profile data: {ex.Message}");
        }
    }
}
