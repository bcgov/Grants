using Grants.ApplicantPortal.API.Core.DTOs;
using Grants.ApplicantPortal.API.Core.Plugins;
using Grants.ApplicantPortal.API.Core.Services;

namespace Grants.ApplicantPortal.API.Infrastructure.Services;

/// <summary>
/// Organization management service that resolves to the appropriate plugin for organization operations.
/// Validates resource ownership before delegating to the plugin to prevent IDOR attacks.
/// </summary>
public class OrganizationManagementService(
  IProfilePluginFactory pluginFactory,
  IResourceOwnershipValidator ownershipValidator,
  ILogger<OrganizationManagementService> logger) : IOrganizationManagementService
{
  public async Task<Result> EditOrganizationAsync(
    EditOrganizationRequest editRequest,
    ProfileContext profileContext,
    CancellationToken cancellationToken = default)
  {
    logger.LogInformation("Editing organization {OrganizationId} for ProfileId: {ProfileId}, Plugin: {PluginId}",
      editRequest.OrganizationId, profileContext.ProfileId, profileContext.PluginId);

    try
    {
      // Resolve the plugin
      var plugin = pluginFactory.GetPlugin(profileContext.PluginId);
      if (plugin == null)
      {
        logger.LogWarning("Plugin not found: {PluginId}", profileContext.PluginId);
        return Result.NotFound($"Plugin '{profileContext.PluginId}' not found");
      }

      // Check if the plugin supports organization management
      if (plugin is not IOrganizationManagementPlugin organizationPlugin)
      {
        logger.LogWarning("Plugin {PluginId} does not support organization management", profileContext.PluginId);
        return Result.Invalid(new ValidationError
        {
          Identifier = "PluginId",
          ErrorMessage = $"Plugin '{profileContext.PluginId}' does not support organization management"
        });
      }

      // Validate organization ownership before allowing edit
      var ownership = await ownershipValidator.ValidateOrganizationOwnershipAsync(
        editRequest.OrganizationId, profileContext, cancellationToken);
      if (!ownership.IsOwned)
      {
        logger.LogWarning("Organization ownership validation failed for OrganizationId: {OrganizationId}, ProfileId: {ProfileId}",
          editRequest.OrganizationId, profileContext.ProfileId);
        return Result.Forbidden();
      }

      // Call the plugin to edit the organization
      var result = await organizationPlugin.EditOrganizationAsync(editRequest, profileContext, cancellationToken);

      if (result.IsSuccess)
      {
        logger.LogInformation("Successfully edited organization {OrganizationId} for ProfileId: {ProfileId}",
          editRequest.OrganizationId, profileContext.ProfileId);
      }
      else
      {
        logger.LogError("Failed to edit organization {OrganizationId} for ProfileId: {ProfileId}. Error: {Error}",
          editRequest.OrganizationId, profileContext.ProfileId, result.Status);
      }

      return result;
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Error editing organization {OrganizationId} for ProfileId: {ProfileId}, Plugin: {PluginId}",
        editRequest.OrganizationId, profileContext.ProfileId, profileContext.PluginId);
      return Result.Error("An unexpected error occurred while editing the organization");
    }
  }
}
