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
public partial class DemoPlugin : IProfilePlugin, IContactManagementPlugin, IAddressManagementPlugin, IOrganizationManagementPlugin
{
    private readonly ILogger<DemoPlugin> _logger;
    private readonly IMessagePublisher? _messagePublisher; // Optional for messaging
    private readonly IDistributedCache _distributedCache; // Direct Redis access only
    private readonly IOptions<ProfileCacheOptions> _cacheOptions;
    private readonly IConnectionMultiplexer? _connectionMultiplexer; // For Redis verification

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
        _connectionMultiplexer = connectionMultiplexer;
    }

    public string PluginId => "DEMO";

    private static readonly IReadOnlyList<PluginSupportedFeature> SupportedFeatures = new List<PluginSupportedFeature>
    {
        // Legacy keys for backwards compatibility
        new("PROGRAM1", "SUBMISSIONS", "Demo submissions data for Program1"),
        new("PROGRAM1", "ORGINFO", "Demo organization information for Program1"),
        new("PROGRAM1", "PAYMENTS", "Demo payment information for Program1"),
        new("PROGRAM2", "SUBMISSIONS", "Demo submissions data for Program2"),
        new("PROGRAM2", "ORGINFO", "Demo organization information for Program2"),
        
        // New specific endpoint keys
        new("PROGRAM1", "CONTACTS", "Demo contacts data for Program1"),
        new("PROGRAM1", "ADDRESSES", "Demo address data for Program1"),
        new("PROGRAM2", "CONTACTS", "Demo contacts data for Program2"),
        new("PROGRAM2", "ADDRESSES", "Demo address data for Program2")
    };

    public IReadOnlyList<PluginSupportedFeature> GetSupportedFeatures()
    {
        return SupportedFeatures;
    }

    public IReadOnlyList<string> GetSupportedProviders()
    {
        return [.. SupportedFeatures
            .Select(f => f.Provider)
            .Distinct(StringComparer.OrdinalIgnoreCase)];
    }

    public IReadOnlyList<string> GetSupportedKeys(string provider)
    {
        if (string.IsNullOrWhiteSpace(provider))
            return [];

        return [.. SupportedFeatures
            .Where(f => f.Provider.Equals(provider, StringComparison.OrdinalIgnoreCase))
            .Select(f => f.Key)];
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

    /// <summary>
    /// Seeds demo data into cache on startup - this is our persistent demo "database"
    /// Note: This does not clear existing data. Use the ClearRedisCache system endpoint for data reset.
    /// </summary>
    public async Task SeedDataAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("=== Seeding DEMO plugin data ===");

        // Verify Redis connectivity if available
        if (_connectionMultiplexer != null)
        {
            try
            {
                var database = _connectionMultiplexer.GetDatabase(1);
                var testResult = await database.StringSetAsync("test:connectivity", "ok", TimeSpan.FromSeconds(5));
                
                if (!testResult)
                {
                    _logger.LogError("Redis connectivity test failed - skipping DEMO data seeding");
                    return;
                }
                
                await database.KeyDeleteAsync("test:connectivity");
                _logger.LogDebug("Redis connectivity verified");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Redis connectivity test failed - skipping DEMO data seeding");
                return;
            }
        }
        else
        {
            _logger.LogInformation("Redis not available - skipping DEMO data seeding");
            return;
        }

        // Pre-defined test profile IDs for consistent demo data
        var testProfileIds = new[]
        {
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            Guid.Parse("33333333-3333-3333-3333-333333333333")
        };

        var scenarios = SupportedFeatures.ToList();
        var seededCount = 0;
        var errors = 0;

        foreach (var profileId in testProfileIds)
        {
            foreach (var feature in scenarios)
            {
                try
                {
                    var cacheKey = $"{_cacheOptions.Value.CacheKeyPrefix}{profileId}:DEMO:{feature.Provider}:{feature.Key}";

                    var metadata = new ProfilePopulationMetadata(
                        profileId, 
                        PluginId, 
                        feature.Provider, 
                        feature.Key, 
                        null);

                    var mockData = GenerateSeedingMockData(metadata);
                    var jsonData = JsonSerializer.Serialize(mockData, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                        WriteIndented = false // Reduced size for caching
                    });

                    var profileData = new ProfileData(
                        profileId,
                        PluginId,
                        feature.Provider,
                        feature.Key,
                        jsonData);

                    // Store in distributed cache with 1-year expiration
                    var profileDataBytes = JsonSerializer.SerializeToUtf8Bytes(profileData);
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
                        profileId, feature.Provider, feature.Key);
                    errors++;
                }
            }
        }

        _logger.LogInformation("DEMO plugin seeding completed: {SeededCount} items seeded, {ErrorCount} errors", 
            seededCount, errors);

        // Quick verification of seeded data
        if (_connectionMultiplexer?.IsConnected == true)
        {
            try
            {
                var database = _connectionMultiplexer.GetDatabase(1);
                var server = _connectionMultiplexer.GetServer(_connectionMultiplexer.GetEndPoints().First());
                var keyCount = server.Keys(database: 1, pattern: "ApplicantPortal*").Count();
                
                _logger.LogInformation("Redis verification: {KeyCount} total keys in database 1", keyCount);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Redis verification failed - this is not critical");
            }
        }
    }

    /// <summary>
    /// Generates consistent mock data for seeding (no DateTime.UtcNow calls)
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
            // Use dedicated data classes for each type
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

