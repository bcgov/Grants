using Grants.ApplicantPortal.API.Core.DTOs;
using Grants.ApplicantPortal.API.Core.Plugins;
using Grants.ApplicantPortal.API.Core.Services;

namespace Grants.ApplicantPortal.API.Infrastructure.Services;

/// <summary>
/// Contact management service that resolves to the appropriate plugin for contact operations
/// </summary>
public class ContactManagementService(
  IProfilePluginFactory pluginFactory,
  ILogger<ContactManagementService> logger) : IContactManagementService
{
  public async Task<Result<Guid>> CreateContactAsync(
    CreateContactRequest contactRequest,
    ProfileContext profileContext,
    CancellationToken cancellationToken = default)
  {
    logger.LogInformation("Creating contact for ProfileId: {ProfileId}, Plugin: {PluginId}, Provider: {Provider}",
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

      // Check if the plugin supports contact management
      if (plugin is not IContactManagementPlugin contactPlugin)
      {
        logger.LogWarning("Plugin {PluginId} does not support contact management", profileContext.PluginId);
        return Result<Guid>.Invalid(new ValidationError 
        { 
          Identifier = "PluginId",
          ErrorMessage = $"Plugin '{profileContext.PluginId}' does not support contact management"
        });
      }

      // Call the plugin to create the contact
      var result = await contactPlugin.CreateContactAsync(contactRequest, profileContext, cancellationToken);

      if (result.IsSuccess)
      {
        logger.LogInformation("Successfully created contact {ContactId} for ProfileId: {ProfileId}",
          result.Value, profileContext.ProfileId);
      }
      else
      {
        logger.LogError("Failed to create contact for ProfileId: {ProfileId}. Error: {Error}",
          profileContext.ProfileId, result.Status);
      }

      return result;
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Error creating contact for ProfileId: {ProfileId}, Plugin: {PluginId}",
        profileContext.ProfileId, profileContext.PluginId);
      return Result<Guid>.Error("An unexpected error occurred while creating the contact");
    }
  }

  public async Task<Result> EditContactAsync(
    EditContactRequest editRequest,
    ProfileContext profileContext,
    CancellationToken cancellationToken = default)
  {
    logger.LogInformation("Editing contact {ContactId} for ProfileId: {ProfileId}, Plugin: {PluginId}",
      editRequest.ContactId, profileContext.ProfileId, profileContext.PluginId);

    try
    {
      // Resolve the plugin
      var plugin = pluginFactory.GetPlugin(profileContext.PluginId);
      if (plugin == null)
      {
        logger.LogWarning("Plugin not found: {PluginId}", profileContext.PluginId);
        return Result.NotFound($"Plugin '{profileContext.PluginId}' not found");
      }

      // Check if the plugin supports contact management
      if (plugin is not IContactManagementPlugin contactPlugin)
      {
        logger.LogWarning("Plugin {PluginId} does not support contact management", profileContext.PluginId);
        return Result.Invalid(new ValidationError
        {
          Identifier = "PluginId",
          ErrorMessage = $"Plugin '{profileContext.PluginId}' does not support contact management"
        });
      }

      // Call the plugin to edit the contact
      var result = await contactPlugin.EditContactAsync(editRequest, profileContext, cancellationToken);

      if (result.IsSuccess)
      {
        logger.LogInformation("Successfully edited contact {ContactId} for ProfileId: {ProfileId}",
          editRequest.ContactId, profileContext.ProfileId);
      }
      else
      {
        logger.LogError("Failed to edit contact {ContactId} for ProfileId: {ProfileId}. Error: {Error}",
          editRequest.ContactId, profileContext.ProfileId, result.Status);
      }

      return result;
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Error editing contact {ContactId} for ProfileId: {ProfileId}, Plugin: {PluginId}",
        editRequest.ContactId, profileContext.ProfileId, profileContext.PluginId);
      return Result.Error("An unexpected error occurred while editing the contact");
    }
  }

  public async Task<Result> SetAsPrimaryContactAsync(
    Guid contactId,
    ProfileContext profileContext,
    CancellationToken cancellationToken = default)
  {
    logger.LogInformation("Setting contact {ContactId} as primary for ProfileId: {ProfileId}, Plugin: {PluginId}",
      contactId, profileContext.ProfileId, profileContext.PluginId);

    try
    {
      // Resolve the plugin
      var plugin = pluginFactory.GetPlugin(profileContext.PluginId);
      if (plugin == null)
      {
        logger.LogWarning("Plugin not found: {PluginId}", profileContext.PluginId);
        return Result.NotFound($"Plugin '{profileContext.PluginId}' not found");
      }

      // Check if the plugin supports contact management
      if (plugin is not IContactManagementPlugin contactPlugin)
      {
        logger.LogWarning("Plugin {PluginId} does not support contact management", profileContext.PluginId);
        return Result.Invalid(new ValidationError
        {
          Identifier = "PluginId",
          ErrorMessage = $"Plugin '{profileContext.PluginId}' does not support contact management"
        });
      }

      // Call the plugin to set the contact as primary
      var result = await contactPlugin.SetAsPrimaryContactAsync(contactId, profileContext, cancellationToken);

      if (result.IsSuccess)
      {
        logger.LogInformation("Successfully set contact {ContactId} as primary for ProfileId: {ProfileId}",
          contactId, profileContext.ProfileId);
      }
      else
      {
        logger.LogError("Failed to set contact {ContactId} as primary for ProfileId: {ProfileId}. Error: {Error}",
          contactId, profileContext.ProfileId, result.Status);
      }

      return result;
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Error setting contact {ContactId} as primary for ProfileId: {ProfileId}, Plugin: {PluginId}",
        contactId, profileContext.ProfileId, profileContext.PluginId);
      return Result.Error("An unexpected error occurred while setting the contact as primary");
    }
  }

  public async Task<Result> DeleteContactAsync(
    Guid contactId,
    ProfileContext profileContext,
    CancellationToken cancellationToken = default)
  {
    logger.LogInformation("Deleting contact {ContactId} for ProfileId: {ProfileId}, Plugin: {PluginId}",
      contactId, profileContext.ProfileId, profileContext.PluginId);

    try
    {
      // Resolve the plugin
      var plugin = pluginFactory.GetPlugin(profileContext.PluginId);
      if (plugin == null)
      {
        logger.LogWarning("Plugin not found: {PluginId}", profileContext.PluginId);
        return Result.NotFound($"Plugin '{profileContext.PluginId}' not found");
      }

      // Check if the plugin supports contact management
      if (plugin is not IContactManagementPlugin contactPlugin)
      {
        logger.LogWarning("Plugin {PluginId} does not support contact management", profileContext.PluginId);
        return Result.Invalid(new ValidationError
        {
          Identifier = "PluginId",
          ErrorMessage = $"Plugin '{profileContext.PluginId}' does not support contact management"
        });
      }

      // Call the plugin to delete the contact
      var result = await contactPlugin.DeleteContactAsync(contactId, profileContext, cancellationToken);

      if (result.IsSuccess)
      {
        logger.LogInformation("Successfully deleted contact {ContactId} for ProfileId: {ProfileId}",
          contactId, profileContext.ProfileId);
      }
      else
      {
        logger.LogError("Failed to delete contact {ContactId} for ProfileId: {ProfileId}. Error: {Error}",
          contactId, profileContext.ProfileId, result.Status);
      }

      return result;
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Error deleting contact {ContactId} for ProfileId: {ProfileId}, Plugin: {PluginId}",
        contactId, profileContext.ProfileId, profileContext.PluginId);
      return Result.Error("An unexpected error occurred while deleting the contact");
    }
  }
}
