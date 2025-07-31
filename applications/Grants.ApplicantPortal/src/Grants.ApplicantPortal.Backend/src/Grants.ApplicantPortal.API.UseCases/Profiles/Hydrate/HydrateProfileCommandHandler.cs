using Grants.ApplicantPortal.API.Core.Plugins;

namespace Grants.ApplicantPortal.API.UseCases.Profiles.Hydrate;

/// <summary>
/// Command handler for hydrating profile data using plugins
/// </summary>
public class HydrateProfileCommandHandler(
    IProfilePluginFactory pluginFactory,
    IDistributedCache distributedCache,
    ILogger<HydrateProfileCommandHandler> logger)
    : ICommandHandler<HydrateProfileCommand, Result<ProfileData>>
{
    private const string CACHE_KEY_PREFIX = "profile:";
    
    public async Task<Result<ProfileData>> Handle(HydrateProfileCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Processing profile hydration for ProfileId: {ProfileId}, PluginId: {PluginId}, Provider: {Provider}, Key: {Key}", 
            request.ProfileId, request.PluginId, request.Provider, request.Key);

        // Get the appropriate plugin
        var plugin = pluginFactory.GetPlugin(request.PluginId);
        if (plugin == null)
        {
            logger.LogError("No plugin found for plugin ID: {PluginId}", request.PluginId);
            return Result.NotFound($"No plugin found for plugin ID: {request.PluginId}");
        }

        try
        {
            // Create metadata for the plugin
            var metadata = new ProfilePopulationMetadata(
                request.ProfileId,
                request.PluginId,
                request.Provider,
                request.Key,
                request.AdditionalData);

            // Validate plugin can handle this request
            if (!plugin.CanHandle(metadata))
            {
                logger.LogWarning("Plugin {PluginId} cannot handle the provided metadata", request.PluginId);
                return Result.Invalid(new ValidationError($"Plugin {request.PluginId} cannot handle the provided metadata"));
            }

            // Hydrate profile data using the plugin
            var profileData = await plugin.PopulateProfileAsync(metadata, cancellationToken);

            // Cache the result with ProfileId, PluginId, Provider, and Key in the cache key
            var cacheKey = $"{CACHE_KEY_PREFIX}{request.ProfileId}:{request.PluginId}:{request.Provider}:{request.Key}";
            var serializedData = JsonSerializer.Serialize(profileData);
            
            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30),
                SlidingExpiration = TimeSpan.FromMinutes(10)
            };

            await distributedCache.SetStringAsync(cacheKey, serializedData, cacheOptions, cancellationToken);

            logger.LogInformation("Profile data hydrated and cached for ProfileId: {ProfileId}, PluginId: {PluginId}, Provider: {Provider}, Key: {Key}", 
                request.ProfileId, request.PluginId, request.Provider, request.Key);
            
            return Result.Success(profileData);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error hydrating profile for ProfileId: {ProfileId}, PluginId: {PluginId}, Provider: {Provider}, Key: {Key}", 
                request.ProfileId, request.PluginId, request.Provider, request.Key);
            return Result.Error($"Failed to hydrate profile: {ex.Message}");
        }
    }
}
