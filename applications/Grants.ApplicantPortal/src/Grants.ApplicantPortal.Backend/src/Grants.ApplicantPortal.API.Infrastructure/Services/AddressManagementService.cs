using Grants.ApplicantPortal.API.Core.DTOs;
using Grants.ApplicantPortal.API.Core.Plugins;
using Grants.ApplicantPortal.API.Core.Services;

namespace Grants.ApplicantPortal.API.Infrastructure.Services;

/// <summary>
/// Address management service that resolves to the appropriate plugin for address operations.
/// Validates resource ownership before delegating to the plugin to prevent IDOR attacks.
/// </summary>
public class AddressManagementService(
  IProfilePluginFactory pluginFactory,
  IResourceOwnershipValidator ownershipValidator,
  ILogger<AddressManagementService> logger) : IAddressManagementService
{
  public async Task<Result<Guid>> CreateAddressAsync(
    CreateAddressRequest addressRequest,
    ProfileContext profileContext,
    CancellationToken cancellationToken = default)
  {
    logger.LogInformation("Creating address for ProfileId: {ProfileId}, Plugin: {PluginId}, Provider: {Provider}",
      profileContext.ProfileId, profileContext.PluginId, profileContext.Provider);

    try
    {
      // Resolve the plugin
      var plugin = pluginFactory.GetPlugin(profileContext.PluginId);
      if (plugin == null)
      {
        logger.LogWarning("Plugin not found: {PluginId}", profileContext.PluginId);
        return Result<Guid>.NotFound($"Plugin '{profileContext.PluginId}' not found");
      }

      // Check if the plugin supports address management
      if (plugin is not IAddressManagementPlugin addressPlugin)
      {
        logger.LogWarning("Plugin {PluginId} does not support address management", profileContext.PluginId);
        return Result<Guid>.Invalid(new ValidationError
        {
          Identifier = "PluginId",
          ErrorMessage = $"Plugin '{profileContext.PluginId}' does not support address management"
        });
      }

      // Validate applicantId ownership
      if (addressRequest.ApplicantId != Guid.Empty)
      {
        var ownership = await ownershipValidator.ValidateApplicantOwnershipAsync(
          addressRequest.ApplicantId, profileContext, cancellationToken);
        if (!ownership.IsOwned)
        {
          logger.LogWarning("Ownership validation failed for address create. ApplicantId: {ApplicantId}, ProfileId: {ProfileId}",
            addressRequest.ApplicantId, profileContext.ProfileId);
          return Result<Guid>.Forbidden();
        }
      }

      // Call the plugin to create the address
      var result = await addressPlugin.CreateAddressAsync(addressRequest, profileContext, cancellationToken);

      if (result.IsSuccess)
      {
        logger.LogInformation("Successfully created address {AddressId} for ProfileId: {ProfileId}",
          result.Value, profileContext.ProfileId);
      }
      else
      {
        logger.LogError("Failed to create address for ProfileId: {ProfileId}. Error: {Error}",
          profileContext.ProfileId, result.Status);
      }

      return result;
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Error creating address for ProfileId: {ProfileId}, Plugin: {PluginId}",
        profileContext.ProfileId, profileContext.PluginId);
      return Result<Guid>.Error("An unexpected error occurred while creating the address");
    }
  }

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

      // Validate address ownership and editability
      var ownership = await ownershipValidator.ValidateAddressOwnershipAsync(
        editRequest.AddressId, profileContext, cancellationToken);
      if (!ownership.IsOwned)
      {
        logger.LogWarning("Ownership validation failed for address edit. AddressId: {AddressId}, ProfileId: {ProfileId}",
          editRequest.AddressId, profileContext.ProfileId);
        return Result.Forbidden();
      }
      if (!ownership.IsEditable)
      {
        logger.LogWarning("Address {AddressId} is not editable for ProfileId: {ProfileId}",
          editRequest.AddressId, profileContext.ProfileId);
        return Result.Invalid(new ValidationError
        {
          Identifier = "AddressId",
          ErrorMessage = "This address is linked to a submission and cannot be modified"
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

      // Validate address ownership
      var ownership = await ownershipValidator.ValidateAddressOwnershipAsync(
        addressId, profileContext, cancellationToken);
      if (!ownership.IsOwned)
      {
        logger.LogWarning("Ownership validation failed for set-primary address. AddressId: {AddressId}, ProfileId: {ProfileId}",
          addressId, profileContext.ProfileId);
        return Result.Forbidden();
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

  public async Task<Result> DeleteAddressAsync(
    Guid addressId,
    Guid applicantId,
    ProfileContext profileContext,
    CancellationToken cancellationToken = default)
  {
    logger.LogInformation("Deleting address {AddressId} for ProfileId: {ProfileId}, Plugin: {PluginId}",
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

      // Validate address ownership and editability
      var ownership = await ownershipValidator.ValidateAddressOwnershipAsync(
        addressId, profileContext, cancellationToken);
      if (!ownership.IsOwned)
      {
        logger.LogWarning("Ownership validation failed for address delete. AddressId: {AddressId}, ProfileId: {ProfileId}",
          addressId, profileContext.ProfileId);
        return Result.Forbidden();
      }
      if (!ownership.IsEditable)
      {
        logger.LogWarning("Address {AddressId} is not editable (delete blocked) for ProfileId: {ProfileId}",
          addressId, profileContext.ProfileId);
        return Result.Invalid(new ValidationError
        {
          Identifier = "AddressId",
          ErrorMessage = "This address is linked to a submission and cannot be deleted"
        });
      }

      // Call the plugin to delete the address
      var result = await addressPlugin.DeleteAddressAsync(addressId, applicantId, profileContext, cancellationToken);

      if (result.IsSuccess)
      {
        logger.LogInformation("Successfully deleted address {AddressId} for ProfileId: {ProfileId}",
          addressId, profileContext.ProfileId);
      }
      else
      {
        logger.LogError("Failed to delete address {AddressId} for ProfileId: {ProfileId}. Error: {Error}",
          addressId, profileContext.ProfileId, result.Status);
      }

      return result;
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Error deleting address {AddressId} for ProfileId: {ProfileId}, Plugin: {PluginId}",
        addressId, profileContext.ProfileId, profileContext.PluginId);
      return Result.Error("An unexpected error occurred while deleting the address");
    }
  }
}
