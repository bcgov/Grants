namespace Grants.ApplicantPortal.API.Web.Plugins;

public record RetrieveProvidersResponse(
    string PluginId,
    IReadOnlyList<ProviderDto> Providers
);

public record ProviderDto(
    string Id,
    string Name,
    Dictionary<string, string> metaData
);
