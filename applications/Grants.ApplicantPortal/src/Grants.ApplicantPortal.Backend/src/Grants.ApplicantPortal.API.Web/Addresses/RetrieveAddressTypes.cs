using Grants.ApplicantPortal.API.Core.Plugins;
using Grants.ApplicantPortal.API.Plugins;
using Grants.ApplicantPortal.API.Web.Auth;

namespace Grants.ApplicantPortal.API.Web.Addresses;

/// <summary>
/// Retrieves the available address type options for a given plugin.
/// </summary>
public class RetrieveAddressTypes(ILogger<RetrieveAddressTypes> logger, IProfilePluginFactory pluginFactory)
  : Endpoint<RetrieveAddressTypesRequest, RetrieveAddressTypesResponse>
{
  public override void Configure()
  {
    Get(RetrieveAddressTypesRequest.Route);
    Policies(AuthPolicies.RequireAuthenticatedUser);
    Summary(s =>
    {
      s.Summary = "Retrieve address type options for a plugin";
      s.Description = "Returns the list of available address type options for the specified plugin. These are used to populate type selectors when creating or editing addresses.";
      s.Responses[200] = "Address types retrieved successfully";
      s.Responses[401] = "Unauthorized - valid JWT token required";
      s.Responses[400] = "Invalid request or plugin validation failed";
      s.Responses[404] = "Plugin not found or not enabled";
    });

    Tags("Addresses", "Plugins");
  }

  public override async Task HandleAsync(RetrieveAddressTypesRequest request, CancellationToken ct)
  {
    var configuredPlugin = PluginRegistry.GetConfiguredPlugin(request.PluginId);

    if (configuredPlugin is null)
    {
      logger.LogWarning("Plugin '{PluginId}' not found or not enabled", request.PluginId);
      await SendNotFoundAsync(ct);
      return;
    }

    var plugin = pluginFactory.GetPlugin(request.PluginId);

    if (plugin is null)
    {
      logger.LogWarning("Plugin '{PluginId}' could not be resolved", request.PluginId);
      await SendNotFoundAsync(ct);
      return;
    }

    var types = plugin.GetAddressTypes();

    logger.LogInformation("Returning {TypeCount} address types for plugin '{PluginId}'",
      types.Count, request.PluginId);

    Response = new RetrieveAddressTypesResponse(
      request.PluginId,
      types.Select(t => new AddressTypeDto(t.Key, t.Label)).ToList());

    await Task.CompletedTask;
  }
}
