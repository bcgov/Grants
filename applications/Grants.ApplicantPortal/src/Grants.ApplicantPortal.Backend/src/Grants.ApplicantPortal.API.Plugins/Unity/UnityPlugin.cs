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

    private static readonly IReadOnlyList<string> _supportedFeatures =
    [
        "ProfilePopulation",
        "ContactManagement",
        "AddressManagement",
        "OrganizationManagement"
    ];

    public IReadOnlyList<string> GetSupportedFeatures() => _supportedFeatures;

    public bool CanHandle(ProfilePopulationMetadata metadata)
    {
        return metadata.PluginId.Equals(PluginId, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Fetches available providers (tenants) from the Unity external API
    /// </summary>
    public async Task<IReadOnlyList<ProviderInfo>> GetProvidersAsync(Guid profileId, string subject, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching providers from Unity external API for ProfileId: {ProfileId}", profileId);

        var request = new ExternalServiceRequest
        {
            Endpoint = "/api/app/applicant-profiles/tenants",
            Method = HttpMethod.Get,
            QueryParameters = new Dictionary<string, string>
            {
                ["ProfileId"] = profileId.ToString(),
                ["Subject"] = subject
            }
        };

        var response = await _externalServiceClient.CallAsync<List<ProviderInfo>>(
            PluginId, request, cancellationToken);

        if (!response.IsSuccess)
        {
            _logger.LogError("Failed to fetch providers from Unity API: {Error} (StatusCode: {StatusCode})",
                response.ErrorMessage, response.StatusCode);

            throw new InvalidOperationException(
                $"Unable to retrieve providers from Unity API: {response.ErrorMessage}");
        }

        _logger.LogInformation("Retrieved {Count} providers from Unity API", response.Data?.Count ?? 0);
        return response.Data ?? [];
    }
}
