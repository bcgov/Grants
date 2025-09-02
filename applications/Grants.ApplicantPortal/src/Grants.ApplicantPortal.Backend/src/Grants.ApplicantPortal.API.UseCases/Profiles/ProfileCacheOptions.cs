namespace Grants.ApplicantPortal.API.UseCases.Profiles;

/// <summary>
/// Configuration options for profile caching behavior using HybridCache
/// </summary>
public class ProfileCacheOptions
{
    public const string SectionName = "ProfileCache";

    /// <summary>
    /// Cache key prefix for profile data
    /// </summary>
    public string CacheKeyPrefix { get; set; } = "profile:";

    /// <summary>
    /// Cache expiry time in minutes (absolute expiration for L2 cache)
    /// </summary>
    public int CacheExpiryMinutes { get; set; } = 30;

    /// <summary>
    /// L1 cache expiry time in minutes (local in-memory cache)
    /// </summary>
    public int SlidingExpiryMinutes { get; set; } = 10;
}
