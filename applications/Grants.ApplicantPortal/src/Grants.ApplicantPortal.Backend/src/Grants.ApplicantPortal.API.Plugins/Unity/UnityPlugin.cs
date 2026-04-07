using Grants.ApplicantPortal.API.Core;
using Grants.ApplicantPortal.API.Core.Plugins;
using Grants.ApplicantPortal.API.Infrastructure.Messaging.Abstractions;
using Grants.ApplicantPortal.API.UseCases;

namespace Grants.ApplicantPortal.API.Plugins.Unity;

/// <summary>
/// Unity profile plugin for populating profile data from Unity systems
/// </summary>
public partial class UnityPlugin(
    ILogger<UnityPlugin> logger,
    IExternalServiceClient externalServiceClient,
    IPluginCacheService pluginCacheService,
    IMessagePublisher? messagePublisher = null) : IProfilePlugin, 
  IContactManagementPlugin, 
  IAddressManagementPlugin, 
  IOrganizationManagementPlugin
{
  public string PluginId => "UNITY";

    private static readonly IReadOnlyList<string> _supportedFeatures =
    [
        "ProfilePopulation",
        "ContactManagement",
        "AddressManagement",
        "OrganizationManagement"
    ];

    public IReadOnlyList<string> GetSupportedFeatures() => _supportedFeatures;

    private static readonly IReadOnlyList<ContactRoleOption> _contactRoles =
    [
        new("General", "General"),
        new("Primary", "Primary Contact"),
        new("Financial", "Financial Officer"),
        new("SigningAuthority", "Additional Signing Authority"),
        new("Executive", "Executive")
    ];

    public IReadOnlyList<ContactRoleOption> GetContactRoles() => _contactRoles;

    public bool CanHandle(ProfilePopulationMetadata metadata)
    {
        return metadata.PluginId.Equals(PluginId, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Represents a tenant returned by the Unity tenants API.
    /// </summary>
    private record UnityTenantDto(string TenantId, string TenantName, Dictionary<string, string> Metadata);

    /// <summary>
    /// Fetches available providers (tenants) from the Unity external API.
    /// Results are cached per profile to avoid repeated upstream calls.
    /// Empty results are not cached so the next request retries the upstream API.
    /// </summary>
    public async Task<IReadOnlyList<ProviderInfo>> GetProvidersAsync(Guid profileId, string subject, CancellationToken cancellationToken = default)
    {
        return await pluginCacheService.GetOrFetchAsync<List<ProviderInfo>>(
            profileId,
            PluginId,
            "providers",
            async ct =>
            {
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

                var response = await externalServiceClient.CallAsync<List<UnityTenantDto>>(
                    PluginId, request, ct);

                if (!response.IsSuccess)
                {
                    logger.LogError("Failed to fetch providers from Unity API: {Error} (StatusCode: {StatusCode})",
                        response.ErrorMessage, response.StatusCode);

                    throw new InvalidOperationException(
                        $"Unable to retrieve providers from Unity API: {response.ErrorMessage}");
                }

                return response.Data?
                    .Select(t => new ProviderInfo(t.TenantId, t.TenantName, t.Metadata))
                    .ToList() ?? [];
            },
            shouldCache: providers => providers.Count > 0,
            cancellationToken);
    }
}
