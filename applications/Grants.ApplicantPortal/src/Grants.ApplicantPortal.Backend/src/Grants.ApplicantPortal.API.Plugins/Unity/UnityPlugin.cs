using Grants.ApplicantPortal.API.Core;
using Grants.ApplicantPortal.API.Core.Plugins;
using Grants.ApplicantPortal.API.Infrastructure.Messaging.Abstractions;
using Grants.ApplicantPortal.API.UseCases;

namespace Grants.ApplicantPortal.API.Plugins.Unity;

/// <summary>
/// Unity profile plugin for populating profile data from Unity systems
/// </summary>
public partial class UnityPlugin : IProfilePlugin, IContactManagementPlugin, IAddressManagementPlugin, IOrganizationManagementPlugin
{
    private readonly ILogger<UnityPlugin> _logger;
    private readonly IExternalServiceClient _externalServiceClient;
    private readonly IMessagePublisher? _messagePublisher; // Optional for messaging
    private readonly IProfileCacheInvalidationService? _cacheInvalidationService; // Optional for cache management

    public UnityPlugin(
        ILogger<UnityPlugin> logger,
        IExternalServiceClient externalServiceClient,
        IMessagePublisher? messagePublisher = null,
        IProfileCacheInvalidationService? cacheInvalidationService = null)
    {
        _logger = logger;
        _externalServiceClient = externalServiceClient;
        _messagePublisher = messagePublisher;
        _cacheInvalidationService = cacheInvalidationService;
    }

    public string PluginId => "UNITY";

    private static readonly IReadOnlyList<PluginSupportedFeature> SupportedFeatures = new List<PluginSupportedFeature>
    {
        new("UNITY", "PROFILE", "Unity user profile data"),
        new("UNITY", "EMPLOYMENT", "Unity employment information"),
        new("UNITY", "SECURITY", "Unity security clearance data"),
        new("UNITY", "CONTACTS", "Unity contact information"),
        new("UNITY", "ADDRESSES", "Unity address information"),
        new("UNITY", "ORGINFO", "Unity organization information")
    };

    public IReadOnlyList<PluginSupportedFeature> GetSupportedFeatures()
    {
        return SupportedFeatures;
    }

    public IReadOnlyList<string> GetSupportedProviders()
    {
        return SupportedFeatures
            .Select(f => f.Provider)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public IReadOnlyList<string> GetSupportedKeys(string provider)
    {
        if (string.IsNullOrWhiteSpace(provider))
            return new List<string>();

        return SupportedFeatures
            .Where(f => f.Provider.Equals(provider, StringComparison.OrdinalIgnoreCase))
            .Select(f => f.Key)
            .ToList();
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
}
