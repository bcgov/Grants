using StackExchange.Redis;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Grants.ApplicantPortal.API.Core.Plugins;
using Grants.ApplicantPortal.API.UseCases;
using Grants.ApplicantPortal.API.Web.Auth;
using Grants.ApplicantPortal.API.Web.Extensions;

namespace Grants.ApplicantPortal.API.Web.System;

/// <summary>
/// Clears all cached profile data for the authenticated user.
/// Works with both Redis and in-memory distributed cache.
/// </summary>
public class ClearCache(
    IDistributedCache distributedCache,
    IProfilePluginFactory pluginFactory,
    IOptions<ProfileCacheOptions> cacheOptions,
    ILogger<ClearCache> logger,
    IConnectionMultiplexer? connectionMultiplexer = null) : EndpointWithoutRequest<ClearCacheResponse>
{
    /// <summary>
    /// Known data keys used across the system.
    /// </summary>
    private static readonly string[] DataKeys = ["CONTACTS", "ADDRESSES", "ORGINFO", "SUBMISSIONS", "PAYMENTS"];

    public override void Configure()
    {
        Post("/System/clear-my-cache");
        Policies(AuthPolicies.RequireAuthenticatedUser);
        Summary(s =>
        {
            s.Summary = "Clear cached profile data for the current user";
            s.Description = "Removes all cached profile data (contacts, addresses, org info, etc.) for the authenticated user. " +
                            "This forces the next request for each data type to re-fetch from the upstream source.";
            s.Responses[200] = "Cache cleared successfully";
            s.Responses[401] = "Unauthorized — valid JWT token required";
        });
        Tags("System");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var profileId = HttpContext.GetRequiredProfileId();
        var subject = HttpContext.User.GetSubject() ?? string.Empty;
        var prefix = cacheOptions.Value.CacheKeyPrefix;

        logger.LogInformation("Clearing cache for ProfileId: {ProfileId}", profileId);

        var result = new ClearCacheResponse { ProfileId = profileId };
        var keysRemoved = new List<string>();

        // 1. Build all known cache keys for this profile and remove via IDistributedCache
        var knownKeys = BuildKnownCacheKeys(profileId, subject, prefix);

        foreach (var key in knownKeys)
        {
            try
            {
                await distributedCache.RemoveAsync(key, ct);
                keysRemoved.Add(key);
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "Failed to remove cache key (may not exist): {Key}", key);
            }
        }

        // 2. If Redis is available, also do a pattern sweep to catch any keys we missed
        if (connectionMultiplexer is { IsConnected: true })
        {
            try
            {
                var database = connectionMultiplexer.GetDatabase();
                var server = connectionMultiplexer.GetServer(connectionMultiplexer.GetEndPoints().First());

                var patterns = new[]
                {
                    $"ApplicantPortal{prefix}{profileId}:*",
                    $"ApplicantPortal{prefix}SEEDED:*:{profileId}",
                    $"ApplicantPortal{prefix}DELETED*:{profileId}:*",
                    $"ApplicantPortal{prefix}LOCK:*:{profileId}"
                };

                foreach (var pattern in patterns)
                {
                    foreach (var key in server.Keys(database.Database, pattern: pattern))
                    {
                        var keyString = key.ToString();
                        if (!keysRemoved.Contains(keyString))
                        {
                            await database.KeyDeleteAsync(key);
                            keysRemoved.Add(keyString);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Redis pattern sweep failed — known keys were still cleared");
            }
        }

        result.Success = true;
        result.KeysCleared = keysRemoved;
        result.KeyCount = keysRemoved.Count;

        logger.LogInformation("Cleared {KeyCount} cache keys for ProfileId: {ProfileId}", keysRemoved.Count, profileId);

        Response = result;
    }

    /// <summary>
    /// Builds all predictable cache keys for a profile across all registered plugins.
    /// </summary>
    private List<string> BuildKnownCacheKeys(Guid profileId, string subject, string prefix)
    {
        var keys = new List<string>();
        var plugins = pluginFactory.GetAllPlugins();

        foreach (var plugin in plugins)
        {
            var pluginId = plugin.PluginId;

            // PluginCacheService keys: {prefix}{profileId}:{pluginId}:{segment}
            keys.Add($"{prefix}{profileId}:{pluginId}:providers");

            // Get providers for this plugin to build provider-scoped keys
            IReadOnlyList<ProviderInfo> providers;
            try
            {
                providers = plugin.GetProvidersAsync(profileId, subject).ConfigureAwait(false).GetAwaiter().GetResult();
            }
            catch
            {
                providers = [];
            }

            foreach (var provider in providers)
            {
                // ProfileDataRetrievalService keys: {prefix}{profileId}:{pluginId}:{provider}:{key}
                foreach (var dataKey in DataKeys)
                {
                    keys.Add($"{prefix}{profileId}:{pluginId}:{provider.Id}:{dataKey}");
                }
            }

            // Demo-specific seeding/deletion flags
            if (pluginId.Equals("DEMO", StringComparison.OrdinalIgnoreCase))
            {
                keys.Add($"{prefix}SEEDED:DEMO:{profileId}");
                keys.Add($"{prefix}LOCK:SEED:DEMO:{profileId}");

                foreach (var provider in providers)
                {
                    foreach (var dataKey in DataKeys)
                    {
                        keys.Add($"{prefix}DELETED:{profileId}:DEMO:{provider.Id}:{dataKey}");
                    }
                }
            }
        }

        return keys;
    }
}

public class ClearCacheResponse
{
    public bool Success { get; set; }
    public Guid ProfileId { get; set; }
    public List<string> KeysCleared { get; set; } = [];
    public int KeyCount { get; set; }
    public string? ErrorMessage { get; set; }
}
