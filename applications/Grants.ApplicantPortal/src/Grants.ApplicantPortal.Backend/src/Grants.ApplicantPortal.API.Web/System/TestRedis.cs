using StackExchange.Redis;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Grants.ApplicantPortal.API.UseCases;
using Grants.ApplicantPortal.API.Core.Plugins;
using System.Diagnostics;
using System.Text.Json;

namespace Grants.ApplicantPortal.API.Web.System;

/// <summary>
/// Diagnostic endpoint to test Redis connectivity, inspect cache keys, and verify DEMO plugin caching
/// </summary>
public class TestRedis : EndpointWithoutRequest<TestRedisResponse>
{
    private readonly IConnectionMultiplexer? _connectionMultiplexer;
    private readonly IDistributedCache _distributedCache;
    private readonly IProfileDataRetrievalService _profileDataRetrievalService;
    private readonly IProfilePluginFactory _pluginFactory;
    private readonly IOptions<ProfileCacheOptions> _cacheOptions;
    private readonly ILogger<TestRedis> _logger;

    public TestRedis(
        IDistributedCache distributedCache,
        IProfileDataRetrievalService profileDataRetrievalService,
        IProfilePluginFactory pluginFactory,
        IOptions<ProfileCacheOptions> cacheOptions,
        ILogger<TestRedis> logger,
        IConnectionMultiplexer? connectionMultiplexer = null)
    {
        _connectionMultiplexer = connectionMultiplexer;
        _distributedCache = distributedCache;
        _profileDataRetrievalService = profileDataRetrievalService;
        _pluginFactory = pluginFactory;
        _cacheOptions = cacheOptions;
        _logger = logger;
    }

    public override void Configure()
    {
        Get("/System/test-redis");
        AllowAnonymous();
        Summary(s => s.Summary = "Test Redis connectivity, inspect cache keys, and verify DEMO plugin caching");
        Tags("System", "Debug");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        _logger.LogInformation("=== Testing Redis Connectivity and DEMO Plugin Caching ===");

        var result = new TestRedisResponse
        {
            RedisConnected = _connectionMultiplexer != null && _connectionMultiplexer.IsConnected,
            Keys = new List<string>(),
            KeyCount = 0,
            DatabaseNumber = -1,
            ConnectionString = "Not available",
            CacheConfiguration = new CacheConfigInfo
            {
                CacheKeyPrefix = _cacheOptions.Value.CacheKeyPrefix,
                CacheExpiryMinutes = _cacheOptions.Value.CacheExpiryMinutes,
                SlidingExpiryMinutes = _cacheOptions.Value.SlidingExpiryMinutes
            },
            DemoPluginTests = new List<DemoPluginTestResult>()
        };

        // Test basic Redis connectivity
        if (_connectionMultiplexer != null)
        {
            try
            {
                var database = _connectionMultiplexer.GetDatabase();
                result.DatabaseNumber = database.Database;
                result.ConnectionString = _connectionMultiplexer.Configuration;

                _logger.LogInformation("Redis connected: {IsConnected}, Database: {Database}, Configuration: {Configuration}", 
                    _connectionMultiplexer.IsConnected, database.Database, _connectionMultiplexer.Configuration);

                // Log connection details for debugging
                var endpoints = _connectionMultiplexer.GetEndPoints();
                foreach (var endpoint in endpoints)
                {
                    var server = _connectionMultiplexer.GetServer(endpoint);
                    _logger.LogInformation("Redis Endpoint: {Endpoint}, Connected: {Connected}", 
                        endpoint, server.IsConnected);
                }

                if (_connectionMultiplexer.IsConnected)
                {
                    // Get server to scan keys
                    var server = _connectionMultiplexer.GetServer(_connectionMultiplexer.GetEndPoints().First());
                    
                    // Scan for profile cache keys with correct prefix
                    var keys = server.Keys(database.Database, pattern: "ApplicantPortalprofile:*").Take(50).ToList();
                    result.Keys = keys.Select(k => k.ToString()).ToList();
                    result.KeyCount = result.Keys.Count;

                    _logger.LogInformation("Found {KeyCount} cache keys in Redis DB {Database}", 
                        result.KeyCount, database.Database);

                    // Test a simple set/get operation with a key you can see in Redis Commander
                    var testKey = "test:redis:connectivity";
                    var testValue = $"Test at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC";
                    
                    await database.StringSetAsync(testKey, testValue, TimeSpan.FromMinutes(1));
                    var retrievedValue = await database.StringGetAsync(testKey);
                    
                    result.TestKeySet = retrievedValue.HasValue && retrievedValue == testValue;
                    result.TestValue = retrievedValue.HasValue ? retrievedValue.ToString() : "null";

                    // Add additional test keys that are easy to spot in Redis Commander
                    var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd-HH-mm-ss");
                    
                    await database.StringSetAsync($"test:demo:timestamp", timestamp, TimeSpan.FromMinutes(10));
                    await database.StringSetAsync($"test:demo:counter", "12345", TimeSpan.FromMinutes(10));
                    await database.StringSetAsync($"test:demo:message", "Hello from Grants Applicant Portal!", TimeSpan.FromMinutes(10));
                    
                    // Test with JSON data similar to profile cache
                    var testProfileData = new
                    {
                        ProfileId = Guid.NewGuid(),
                        TestData = "This is test data for Redis Commander",
                        Timestamp = DateTime.UtcNow,
                        Source = "TestRedis Endpoint"
                    };
                    
                    var jsonTestData = JsonSerializer.Serialize(testProfileData, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                        WriteIndented = true
                    });
                    
                    await database.StringSetAsync($"test:profile:sample", jsonTestData, TimeSpan.FromMinutes(10));

                    // Track test keys created
                    result.TestKeysCreated.AddRange(new[]
                    {
                        "test:redis:connectivity",
                        "test:demo:timestamp", 
                        "test:demo:counter",
                        "test:demo:message",
                        "test:profile:sample"
                    });

                    _logger.LogInformation("Redis test key operation: Success={Success}, Value={Value}", 
                        result.TestKeySet, result.TestValue);
                    
                    _logger.LogInformation("Added test keys: test:demo:timestamp, test:demo:counter, test:demo:message, test:profile:sample");

                    // Test DEMO plugin caching
                    await TestDemoPluginCaching(result, ct);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing Redis connectivity");
                result.ErrorMessage = ex.Message;
            }
        }
        else
        {
            _logger.LogWarning("Redis connection multiplexer is null - Redis not configured");
            result.ErrorMessage = "Redis connection multiplexer not available";
        }

        Response = result;
        await Task.CompletedTask;
    }

    private async Task TestDemoPluginCaching(TestRedisResponse result, CancellationToken ct)
    {
        _logger.LogInformation("=== Testing DEMO Plugin Caching ===");

        // First, test plugin factory directly
        _logger.LogInformation("Testing plugin factory...");
        var availablePlugins = _pluginFactory.GetAllPlugins().ToList();
        _logger.LogInformation("Available plugins: {PluginCount}", availablePlugins.Count);
        
        foreach (var plugin in availablePlugins)
        {
            _logger.LogInformation("- Plugin: {PluginId} ({PluginType})", plugin.PluginId, plugin.GetType().Name);
        }

        var demoPlugin = _pluginFactory.GetPlugin("DEMO");
        if (demoPlugin == null)
        {
            _logger.LogError("DEMO plugin not found in plugin factory!");
            return;
        }
        else
        {
            _logger.LogInformation("DEMO plugin found: {PluginType}", demoPlugin.GetType().Name);
        }

        // Use a consistent test profile ID for demo data
        var testProfileId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var testScenarios = new[]
        {
            new { Provider = "PROGRAM1", Key = "CONTACTS", Description = "Program1 Contacts" },
            new { Provider = "PROGRAM1", Key = "ADDRESSES", Description = "Program1 Addresses" },
            new { Provider = "PROGRAM1", Key = "ORGINFO", Description = "Program1 Organization Info" },
            new { Provider = "PROGRAM2", Key = "CONTACTS", Description = "Program2 Contacts" }
        };

        foreach (var scenario in testScenarios)
        {
            var testResult = new DemoPluginTestResult
            {
                Provider = scenario.Provider,
                Key = scenario.Key,
                Description = scenario.Description,
                ProfileId = testProfileId
            };

            try
            {
                _logger.LogInformation("Testing DEMO plugin caching for {Provider}:{Key}", scenario.Provider, scenario.Key);

                // Test plugin factory again for this specific call
                var pluginForThisCall = _pluginFactory.GetPlugin("DEMO");
                if (pluginForThisCall == null)
                {
                    _logger.LogError("DEMO plugin not found for {Provider}:{Key}!", scenario.Provider, scenario.Key);
                    testResult.ErrorMessage = "DEMO plugin not found in factory";
                    result.DemoPluginTests.Add(testResult);
                    continue;
                }

                var stopwatch = Stopwatch.StartNew();

                // First call - should cache the data
                var firstCallResult = await _profileDataRetrievalService.RetrieveProfileDataAsync(
                    testProfileId, "DEMO", scenario.Provider, scenario.Key, null, ct);

                stopwatch.Stop();
                testResult.FirstCallDurationMs = stopwatch.ElapsedMilliseconds;
                testResult.FirstCallSuccess = firstCallResult.IsSuccess;

                if (firstCallResult.IsSuccess)
                {
                    testResult.DataRetrieved = !string.IsNullOrEmpty(firstCallResult.Value?.ToString());
                    
                    // Second call - should use cache
                    stopwatch.Restart();
                    var secondCallResult = await _profileDataRetrievalService.RetrieveProfileDataAsync(
                        testProfileId, "DEMO", scenario.Provider, scenario.Key, null, ct);
                    stopwatch.Stop();

                    testResult.SecondCallDurationMs = stopwatch.ElapsedMilliseconds;
                    testResult.SecondCallSuccess = secondCallResult.IsSuccess;
                    testResult.CacheUsed = testResult.SecondCallDurationMs < testResult.FirstCallDurationMs;

                    // Generate expected cache key with ApplicantPortal prefix
                    testResult.ExpectedCacheKey = $"ApplicantPortal{_cacheOptions.Value.CacheKeyPrefix}{testProfileId}:DEMO:{scenario.Provider}:{scenario.Key}";

                    _logger.LogInformation("DEMO plugin test for {Provider}:{Key} - First: {FirstMs}ms, Second: {SecondMs}ms, Cache likely used: {CacheUsed}", 
                        scenario.Provider, scenario.Key, testResult.FirstCallDurationMs, testResult.SecondCallDurationMs, testResult.CacheUsed);
                }
                else
                {
                    testResult.ErrorMessage = firstCallResult.Errors?.FirstOrDefault() ?? "Unknown error";
                    _logger.LogWarning("DEMO plugin test failed for {Provider}:{Key} - {Error}", scenario.Provider, scenario.Key, testResult.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                testResult.ErrorMessage = ex.Message;
                _logger.LogError(ex, "Exception during DEMO plugin test for {Provider}:{Key}", scenario.Provider, scenario.Key);
            }

            result.DemoPluginTests.Add(testResult);
        }

        _logger.LogInformation("=== DEMO Plugin Caching Tests Completed ===");
    }
}

public class TestRedisResponse
{
    public bool RedisConnected { get; set; }
    public List<string> Keys { get; set; } = new();
    public int KeyCount { get; set; }
    public int DatabaseNumber { get; set; }
    public string ConnectionString { get; set; } = string.Empty;
    public bool TestKeySet { get; set; }
    public string TestValue { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public CacheConfigInfo CacheConfiguration { get; set; } = new();
    public List<DemoPluginTestResult> DemoPluginTests { get; set; } = new();
    public List<string> TestKeysCreated { get; set; } = new();
}

public class CacheConfigInfo
{
    public string CacheKeyPrefix { get; set; } = string.Empty;
    public int CacheExpiryMinutes { get; set; }
    public int SlidingExpiryMinutes { get; set; }
}

public class DemoPluginTestResult
{
    public string Provider { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid ProfileId { get; set; }
    public bool FirstCallSuccess { get; set; }
    public bool SecondCallSuccess { get; set; }
    public long FirstCallDurationMs { get; set; }
    public long SecondCallDurationMs { get; set; }
    public bool DataRetrieved { get; set; }
    public bool CacheUsed { get; set; }
    public string ExpectedCacheKey { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
}
