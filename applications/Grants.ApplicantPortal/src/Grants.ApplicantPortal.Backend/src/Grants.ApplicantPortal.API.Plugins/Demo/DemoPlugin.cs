using Grants.ApplicantPortal.API.Core.Plugins;
using Grants.ApplicantPortal.API.Infrastructure.Messaging.Abstractions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Grants.ApplicantPortal.API.UseCases;
using Grants.ApplicantPortal.API.Plugins.Demo.Data;
using StackExchange.Redis;
using System.Text.Json;

namespace Grants.ApplicantPortal.API.Plugins.Demo;

/// <summary>
/// Demo profile plugin for testing and demonstration purposes
/// </summary>
public partial class DemoPlugin : IProfilePlugin, 
  IContactManagementPlugin, 
  IAddressManagementPlugin, 
  IOrganizationManagementPlugin
{
    private readonly ILogger<DemoPlugin> _logger;
    private readonly IMessagePublisher? _messagePublisher; // Optional for messaging
    private readonly IDistributedCache _distributedCache; // Direct Redis access only
    private readonly IOptions<ProfileCacheOptions> _cacheOptions;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public DemoPlugin(
        ILogger<DemoPlugin> logger,
        IDistributedCache distributedCache,
        IOptions<ProfileCacheOptions> cacheOptions,
        IMessagePublisher? messagePublisher = null,
        IConnectionMultiplexer? connectionMultiplexer = null)
    {
        _logger = logger;
        _distributedCache = distributedCache;
        _cacheOptions = cacheOptions;
        _messagePublisher = messagePublisher;        
    }

    public string PluginId => "DEMO";

    private static readonly IReadOnlyList<string> _supportedFeatures =
    [
        "ProfilePopulation",
        "ContactManagement",
        "AddressManagement",
        "OrganizationManagement"
    ];

    public IReadOnlyList<string> GetSupportedFeatures() => _supportedFeatures;

    public Task<IReadOnlyList<ProviderInfo>> GetProvidersAsync(Guid profileId, string subject, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<ProviderInfo> providers =
        [
            new("PROGRAM1", "PROGRAM1"),
            new("PROGRAM2", "PROGRAM2")
        ];
        return Task.FromResult(providers);
    }

    public bool CanHandle(ProfilePopulationMetadata metadata)
    {
        return metadata.PluginId.Equals(PluginId, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Internal seed scenarios — provider/key combinations used to populate demo cache data
    /// </summary>
    private record SeedScenario(string Provider, string Key);
    private static readonly SeedScenario[] _seedScenarios =
    [
        new("PROGRAM1", "SUBMISSIONS"),
        new("PROGRAM1", "ORGINFO"),
        new("PROGRAM1", "PAYMENTS"),
        new("PROGRAM1", "CONTACTS"),
        new("PROGRAM1", "ADDRESSES"),
        new("PROGRAM2", "SUBMISSIONS"),
        new("PROGRAM2", "ORGINFO"),
        new("PROGRAM2", "CONTACTS"),
        new("PROGRAM2", "ADDRESSES")
    ];

    /// <summary>
    /// Seeds demo data for a specific user profile on first request with distributed locking
    /// </summary>
    internal async Task SeedDataForProfileAsync(Guid profileId, CancellationToken cancellationToken = default)
    {
        // Check if this profile has already been seeded
        var seedingFlagKey = $"{_cacheOptions.Value.CacheKeyPrefix}SEEDED:DEMO:{profileId}";
        var alreadySeeded = await _distributedCache.GetAsync(seedingFlagKey, cancellationToken);
        
        if (alreadySeeded != null)
        {
            _logger.LogDebug("Demo data already seeded for ProfileId: {ProfileId}", profileId);
            return;
        }

        // Use distributed lock to prevent concurrent seeding for the same profile
        var lockKey = $"{_cacheOptions.Value.CacheKeyPrefix}LOCK:SEED:DEMO:{profileId}";
        var lockValue = Environment.MachineName + "_" + Thread.CurrentThread.ManagedThreadId + "_" + Guid.NewGuid();
        var lockExpiry = TimeSpan.FromMinutes(5); // Lock expires in 5 minutes

        try
        {
            // Try to acquire distributed lock
            var lockAcquired = await TryAcquireDistributedLock(lockKey, lockValue, lockExpiry, cancellationToken);
            
            if (!lockAcquired)
            {
                // Another process is seeding, wait briefly and check if seeding completed
                await Task.Delay(100, cancellationToken);
                var seedCheckAfterWait = await _distributedCache.GetAsync(seedingFlagKey, cancellationToken);
                
                if (seedCheckAfterWait != null)
                {
                    _logger.LogDebug("Demo data seeded by another process for ProfileId: {ProfileId}", profileId);
                    return;
                }
                
                _logger.LogWarning("Could not acquire seeding lock for ProfileId: {ProfileId}, skipping seeding", profileId);
                return;
            }

            // Double-check if seeding is still needed (another process might have completed it)
            var doubleCheckSeeded = await _distributedCache.GetAsync(seedingFlagKey, cancellationToken);
            if (doubleCheckSeeded != null)
            {
                _logger.LogDebug("Demo data was seeded by another process during lock acquisition for ProfileId: {ProfileId}", profileId);
                return;
            }

            _logger.LogInformation("=== Seeding DEMO data for ProfileId: {ProfileId} ===", profileId);

            var scenarios = _seedScenarios;
            var seededCount = 0;
            var errors = 0;

            foreach (var scenario in scenarios)
            {
                try
                {
                    var cacheKey = $"{_cacheOptions.Value.CacheKeyPrefix}{profileId}:DEMO:{scenario.Provider}:{scenario.Key}";

                    // Check if this combination was previously deleted
                    var deletionKey = $"{_cacheOptions.Value.CacheKeyPrefix}DELETED:{profileId}:DEMO:{scenario.Provider}:{scenario.Key}";
                    var wasDeleted = await _distributedCache.GetAsync(deletionKey, cancellationToken);
                    if (wasDeleted != null)
                    {
                        _logger.LogDebug("Skipping seeding for {ProfileId}:{Provider}:{Key} - was previously deleted",
                            profileId, scenario.Provider, scenario.Key);
                        continue; // Skip if this was deleted
                    }

                    var metadata = new ProfilePopulationMetadata(
                        profileId, 
                        PluginId, 
                        scenario.Provider, 
                        scenario.Key, 
                        string.Empty);

                    var mockData = GenerateSeedingMockData(metadata);

                    var profileData = new ProfileData(
                        profileId,
                        PluginId,
                        scenario.Provider,
                        scenario.Key,
                        mockData);

                    // Store in distributed cache with 1-year expiration
                    var profileDataBytes = JsonSerializer.SerializeToUtf8Bytes(profileData, _jsonOptions);
                    var cacheOptions = new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(365)
                    };
                    
                    await _distributedCache.SetAsync(cacheKey, profileDataBytes, cacheOptions, cancellationToken);
                    seededCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to seed data for {ProfileId}:{Provider}:{Key}",
                        profileId, scenario.Provider, scenario.Key);
                    errors++;
                }
            }

            // Mark this profile as seeded
            var seedingFlag = JsonSerializer.SerializeToUtf8Bytes(new
            {
                ProfileId = profileId,
                SeededAt = DateTime.UtcNow,
                SeededBy = PluginId,
                MachineName = Environment.MachineName
            });
            
            var flagCacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(365)
            };
            
            await _distributedCache.SetAsync(seedingFlagKey, seedingFlag, flagCacheOptions, cancellationToken);

            _logger.LogInformation("DEMO plugin seeding completed for ProfileId: {ProfileId} - {SeededCount} items seeded, {ErrorCount} errors", 
                profileId, seededCount, errors);
        }
        finally
        {
            // Always release the lock
            await ReleaseDistributedLock(lockKey, lockValue, cancellationToken);
        }
    }

    /// <summary>
    /// Attempts to acquire a distributed lock using Redis
    /// </summary>
    private async Task<bool> TryAcquireDistributedLock(string lockKey, string lockValue, TimeSpan expiry, CancellationToken cancellationToken)
    {
        try
        {
            var lockData = JsonSerializer.SerializeToUtf8Bytes(new 
            { 
                Value = lockValue, 
                AcquiredAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.Add(expiry)
            });
            
            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiry
            };

            // Try to set the lock only if it doesn't exist (atomic operation)
            var existingLock = await _distributedCache.GetAsync(lockKey, cancellationToken);
            if (existingLock == null)
            {
                await _distributedCache.SetAsync(lockKey, lockData, cacheOptions, cancellationToken);
                
                // Verify we actually got the lock (race condition check)
                await Task.Delay(10, cancellationToken); // Small delay to ensure consistency
                var verifyLock = await _distributedCache.GetAsync(lockKey, cancellationToken);
                if (verifyLock != null)
                {
                    try
                    {
                        var lockInfo = JsonSerializer.Deserialize<dynamic>(verifyLock);
                        var storedValue = lockInfo?.GetProperty("Value").GetString();
                        return storedValue == lockValue;
                    }
                    catch
                    {
                        return false;
                    }
                }
            }
            
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to acquire distributed lock for key: {LockKey}", lockKey);
            return false;
        }
    }

    /// <summary>
    /// Releases a distributed lock if we own it
    /// </summary>
    private async Task ReleaseDistributedLock(string lockKey, string lockValue, CancellationToken cancellationToken)
    {
        try
        {
            var existingLock = await _distributedCache.GetAsync(lockKey, cancellationToken);
            if (existingLock != null)
            {
                try
                {
                    var lockInfo = JsonSerializer.Deserialize<dynamic>(existingLock);
                    var storedValue = lockInfo?.GetProperty("Value").GetString();
                    
                    if (storedValue == lockValue)
                    {
                        await _distributedCache.RemoveAsync(lockKey, cancellationToken);
                        _logger.LogDebug("Released distributed lock for key: {LockKey}", lockKey);
                    }
                }
                catch
                {
                    // If we can't parse the lock, just remove it to be safe
                    await _distributedCache.RemoveAsync(lockKey, cancellationToken);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to release distributed lock for key: {LockKey}", lockKey);
        }
    }

    /// <summary>
    /// Generates consistent mock data for seeding (no DateTime.UtcNow calls)
    /// Returns clean data directly, matching Unity plugin format
    /// </summary>
    private object GenerateSeedingMockData(ProfilePopulationMetadata metadata)
    {
        var baseData = new
        {
            ProfileId = metadata.ProfileId,
            Provider = metadata.Provider,
            Key = metadata.Key,
            Source = "Demo System",
            PopulatedAt = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc), // Fixed timestamp
            PopulatedBy = PluginId
        };

        return (metadata.Provider?.ToUpper(), metadata.Key?.ToUpper()) switch
        {
            // Use dedicated data classes for each type - they now return clean data directly
            ("PROGRAM1", "SUBMISSIONS") => SubmissionsData.GenerateProgram1Submissions(baseData),
            ("PROGRAM1", "ORGINFO") => OrganizationsData.GenerateProgram1OrgInfo(baseData),
            ("PROGRAM1", "PAYMENTS") => OrganizationsData.GenerateProgram1Payments(baseData),
            ("PROGRAM1", "CONTACTS") => ContactsData.GenerateProgram1Contacts(baseData),
            ("PROGRAM1", "ADDRESSES") => AddressesData.GenerateProgram1Addresses(baseData),
            ("PROGRAM2", "SUBMISSIONS") => SubmissionsData.GenerateProgram2Submissions(baseData),
            ("PROGRAM2", "ORGINFO") => OrganizationsData.GenerateProgram2OrgInfo(baseData),
            ("PROGRAM2", "CONTACTS") => ContactsData.GenerateProgram2Contacts(baseData),
            ("PROGRAM2", "ADDRESSES") => AddressesData.GenerateProgram2Addresses(baseData),
            _ => throw new NotImplementedException($"No mock data generator for {metadata.Provider}:{metadata.Key}")
        };
    }
}

