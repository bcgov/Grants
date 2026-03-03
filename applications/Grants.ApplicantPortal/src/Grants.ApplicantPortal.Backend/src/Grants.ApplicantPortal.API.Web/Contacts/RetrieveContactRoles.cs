using Grants.ApplicantPortal.API.Core.Plugins;
using Grants.ApplicantPortal.API.Plugins;
using Grants.ApplicantPortal.API.Web.Auth;

namespace Grants.ApplicantPortal.API.Web.Contacts;

/// <summary>
/// Retrieves the available contact role options for a given plugin.
/// </summary>
public class RetrieveContactRoles(ILogger<RetrieveContactRoles> logger, IProfilePluginFactory pluginFactory)
  : Endpoint<RetrieveContactRolesRequest, RetrieveContactRolesResponse>
{
  public override void Configure()
  {
    Get(RetrieveContactRolesRequest.Route);
    Policies(AuthPolicies.RequireAuthenticatedUser);
    Summary(s =>
    {
      s.Summary = "Retrieve contact role options for a plugin";
      s.Description = "Returns the list of available contact role options for the specified plugin. These are used to populate role selectors when creating or editing contacts.";
      s.Responses[200] = "Contact roles retrieved successfully";
      s.Responses[401] = "Unauthorized - valid JWT token required";
      s.Responses[400] = "Invalid request or plugin validation failed";
      s.Responses[404] = "Plugin not found or not enabled";
    });

    Tags("Contacts", "Plugins");
  }

  public override async Task HandleAsync(RetrieveContactRolesRequest request, CancellationToken ct)
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

    var roles = plugin.GetContactRoles();

    logger.LogInformation("Returning {RoleCount} contact roles for plugin '{PluginId}'",
      roles.Count, request.PluginId);

    Response = new RetrieveContactRolesResponse(
      request.PluginId,
      roles.Select(r => new ContactRoleDto(r.Key, r.Label)).ToList());

    await Task.CompletedTask;
  }
}
