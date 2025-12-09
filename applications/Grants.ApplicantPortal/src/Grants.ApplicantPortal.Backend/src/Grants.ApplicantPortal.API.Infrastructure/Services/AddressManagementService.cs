using Grants.ApplicantPortal.API.Core.DTOs;
using Grants.ApplicantPortal.API.Core.Plugins;
using Grants.ApplicantPortal.API.Core.Services;

namespace Grants.ApplicantPortal.API.Infrastructure.Services;

/// <summary>
/// Address management service that resolves to the appropriate plugin for address operations
/// </summary>
public class AddressManagementService(
  IProfilePluginFactory pluginFactory,
  ILogger<AddressManagementService> logger) : IAddressManagementService
{
  public async Task<Result> EditAddressAsync(
    EditAddressRequest editRequest,
    ProfileContext profileContext,
    CancellationToken cancellationToken = default)
  {
    logger.LogInformation("Editing address {AddressId} for ProfileId: {ProfileId}, Plugin: {PluginId}",
      editRequest.AddressId, profileContext.ProfileId, profileContext.PluginId);

    try
    {
      // Resolve the plugin
      var plugin = pluginFactory.GetPlugin(profileContext.PluginId);
      if (plugin == null)
      {
        logger.LogWarning("Plugin not found: {PluginId}", profileContext.PluginId);
        return Result.NotFound($"Plugin '{profileContext.PluginId}' not found");
      }

      // Check if the plugin supports address management
      if (plugin is not IAddressManagementPlugin addressPlugin)
      {
        logger.LogWarning("Plugin {PluginId} does not support address management", profileContext.PluginId);
        return Result.Invalid(new ValidationError
        {
          Identifier = "PluginId",
          ErrorMessage = $"Plugin '{profileContext.PluginId}' does not support address management"
        });
      }

      // Call the plugin to edit the address
      var result = await addressPlugin.EditAddressAsync(editRequest, profileContext, cancellationToken);

      if (result.IsSuccess)
      {
        logger.LogInformation("Successfully edited address {AddressId} for ProfileId: {ProfileId}",
          editRequest.AddressId, profileContext.ProfileId);
      }
      else
      {
        logger.LogError("Failed to edit address {AddressId} for ProfileId: {ProfileId}. Error: {Error}",
          editRequest.AddressId, profileContext.ProfileId, result.Status);
      }

      return result;
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Error editing address {AddressId} for ProfileId: {ProfileId}, Plugin: {PluginId}",
        editRequest.AddressId, profileContext.ProfileId, profileContext.PluginId);
      return Result.Error("An unexpected error occurred while editing the address");
    }
  }

  public async Task<Result> SetAsPrimaryAddressAsync(
    Guid addressId,
    ProfileContext profileContext,
    CancellationToken cancellationToken = default)
  {
    logger.LogInformation("Setting address {AddressId} as primary for ProfileId: {ProfileId}, Plugin: {PluginId}",
      addressId, profileContext.ProfileId, profileContext.PluginId);

    try
    {
      // Resolve the plugin
      var plugin = pluginFactory.GetPlugin(profileContext.PluginId);
      if (plugin == null)
      {
        logger.LogWarning("Plugin not found: {PluginId}", profileContext.PluginId);
        return Result.NotFound($"Plugin '{profileContext.PluginId}' not found");
      }

      // Check if the plugin supports address management
      if (plugin is not IAddressManagementPlugin addressPlugin)
      {
        logger.LogWarning("Plugin {PluginId} does not support address management", profileContext.PluginId);
        return Result.Invalid(new ValidationError
        {
          Identifier = "PluginId",
          ErrorMessage = $"Plugin '{profileContext.PluginId}' does not support address management"
        });
      }

      // Call the plugin to set the address as primary
      var result = await addressPlugin.SetAsPrimaryAddressAsync(addressId, profileContext, cancellationToken);

      if (result.IsSuccess)
      {
        logger.LogInformation("Successfully set address {AddressId} as primary for ProfileId: {ProfileId}",
          addressId, profileContext.ProfileId);
      }
      else
      {
        logger.LogError("Failed to set address {AddressId} as primary for ProfileId: {ProfileId}. Error: {Error}",
          addressId, profileContext.ProfileId, result.Status);
      }

      return result;
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Error setting address {AddressId} as primary for ProfileId: {ProfileId}, Plugin: {PluginId}",
        addressId, profileContext.ProfileId, profileContext.PluginId);
      return Result.Error("An unexpected error occurred while setting the address as primary");
    }
  }
}
