using Ardalis.Result;
using Grants.ApplicantPortal.API.Core.DTOs;
using Grants.ApplicantPortal.API.Infrastructure.Messaging.Messages;

namespace Grants.ApplicantPortal.API.Plugins.Unity;

/// <summary>
/// Contact management implementation for Unity plugin
/// </summary>
public partial class UnityPlugin
{
  public async Task<Result<Guid>> CreateContactAsync(
      CreateContactRequest contactRequest,
      ProfileContext profileContext,
      CancellationToken cancellationToken = default)
  {
    logger.LogInformation("Unity plugin creating contact for ProfileId: {ProfileId}, Name: {Name}, Type: {Type}",
        profileContext.ProfileId, contactRequest.Name, contactRequest.Type);

    try
    {
      // Generate a new contact ID for the Unity system
      var newContactId = Guid.NewGuid();

      // 🔥 STEP 1: Update cache optimistically with the new contact
      await UpdateContactsCacheOptimistically(newContactId, contactRequest, profileContext, cancellationToken);

      // 🔥 STEP 2: Send command to Unity via message queue
      await FireContactCreateMessage(newContactId, contactRequest, profileContext, cancellationToken);

      logger.LogInformation("Unity plugin optimistically created contact - ID: {ContactId}, Name: {Name}, Type: {Type}, Email: {Email}, Phone: {Phone}",
          newContactId, contactRequest.Name, contactRequest.Type, contactRequest.Email, contactRequest.PhoneNumber);

      return Result<Guid>.Success(newContactId);
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Unity plugin failed to queue contact creation for ProfileId: {ProfileId}, Name: {Name}",
          profileContext.ProfileId, contactRequest.Name);
      return Result<Guid>.Error("Failed to queue contact creation for Unity system");
    }
  }

  public async Task<Result> EditContactAsync(
      EditContactRequest editRequest,
      ProfileContext profileContext,
      CancellationToken cancellationToken = default)
  {
    logger.LogInformation("Unity plugin editing contact {ContactId} for ProfileId: {ProfileId}",
        editRequest.ContactId, profileContext.ProfileId);

    try
    {
      // 🔥 STEP 1: Update cache optimistically with the edited contact
      await UpdateContactsCacheOptimistically(editRequest, profileContext, cancellationToken);

      // 🔥 STEP 2: Send edit command to Unity via message queue
      await FireContactEditMessage(editRequest, profileContext, cancellationToken);

      logger.LogInformation("Unity plugin optimistically edited contact - ID: {ContactId}, Name: {Name}, Type: {Type}",
          editRequest.ContactId, editRequest.Name, editRequest.Type);

      return Result.Success();
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Unity plugin failed to queue contact edit {ContactId} for ProfileId: {ProfileId}",
          editRequest.ContactId, profileContext.ProfileId);
      return Result.Error("Failed to queue contact edit for Unity system");
    }
  }

  public async Task<Result> SetAsPrimaryContactAsync(
      Guid contactId,
      ProfileContext profileContext,
      CancellationToken cancellationToken = default)
  {
    logger.LogInformation("Unity plugin setting contact {ContactId} as primary for ProfileId: {ProfileId}",
        contactId, profileContext.ProfileId);

    try
    {
      // 🔥 EVENT-DRIVEN: Publish set primary command to outbox for Unity to process
      await FireContactSetPrimaryMessage(contactId, profileContext, cancellationToken);

      // 🔥 Invalidate the CONTACTS cache when primary contact change is queued
      await InvalidateContactsCache(profileContext.ProfileId, profileContext.Provider, cancellationToken);

      logger.LogInformation("Unity plugin queued set contact {ContactId} as primary for ProfileId: {ProfileId}",
          contactId, profileContext.ProfileId);

      return Result.Success();
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Unity plugin failed to queue set contact {ContactId} as primary for ProfileId: {ProfileId}",
          contactId, profileContext.ProfileId);
      return Result.Error("Failed to queue set contact as primary for Unity system");
    }
  }

  public async Task<Result> DeleteContactAsync(
      Guid contactId,
      ProfileContext profileContext,
      CancellationToken cancellationToken = default)
  {
    logger.LogInformation("Unity plugin deleting contact {ContactId} for ProfileId: {ProfileId}",
        contactId, profileContext.ProfileId);

    try
    {
      // 🔥 EVENT-DRIVEN: Publish delete command to outbox for Unity to process
      await FireContactDeleteMessage(contactId, profileContext, cancellationToken);

      // 🔥 Invalidate the CONTACTS cache when contact deletion is queued
      await InvalidateContactsCache(profileContext.ProfileId, profileContext.Provider, cancellationToken);

      logger.LogInformation("Unity plugin queued contact deletion {ContactId} for ProfileId: {ProfileId}",
          contactId, profileContext.ProfileId);

      return Result.Success();
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Unity plugin failed to queue contact deletion {ContactId} for ProfileId: {ProfileId}",
          contactId, profileContext.ProfileId);
      return Result.Error("Failed to queue contact deletion for Unity system");
    }
  }

  /// <summary>
  /// Helper method to fire contact create command message
  /// </summary>
  private async Task FireContactCreateMessage(Guid contactId, CreateContactRequest contactRequest, ProfileContext profileContext, CancellationToken cancellationToken)
  {
    if (messagePublisher == null)
    {
      logger.LogError("Message publisher not available - cannot publish critical ContactCreateCommand for contact {ContactId}", contactId);
      throw new InvalidOperationException("Message publisher is required for Unity plugin operations");
    }

    var message = new PluginDataMessage(
        PluginId,
        "CONTACT_CREATE_COMMAND",
        new
        {
          Action = "CreateContact",
          ContactId = contactId,
          profileContext.ProfileId,
          profileContext.Provider,
          Data = new
          {
            contactRequest.Name,
            contactRequest.Email,
            contactRequest.Title,
            contactRequest.Type,
            contactRequest.PhoneNumber,
            contactRequest.IsPrimary
          }
        },
        correlationId: $"profile-{profileContext.ProfileId}");

    await messagePublisher.PublishAsync(message, cancellationToken);

    logger.LogDebug("Published ContactCreateCommand for contact {ContactId} in profile {ProfileId}",
        contactId, profileContext.ProfileId);
  }

  /// <summary>
  /// Helper method to fire contact edit command message
  /// </summary>
  private async Task FireContactEditMessage(EditContactRequest editRequest, ProfileContext profileContext, CancellationToken cancellationToken)
  {
    if (messagePublisher == null)
    {
      logger.LogError("Message publisher not available - cannot publish critical ContactEditCommand for contact {ContactId}", editRequest.ContactId);
      throw new InvalidOperationException("Message publisher is required for Unity plugin operations");
    }

    var message = new PluginDataMessage(
        PluginId,
        "CONTACT_EDIT_COMMAND",
        new
        {
          Action = "EditContact",
          editRequest.ContactId,
          profileContext.ProfileId,
          profileContext.Provider,
          Data = new
          {
            editRequest.Name,
            editRequest.Email,
            editRequest.Title,
            editRequest.Type,
            editRequest.PhoneNumber,
            editRequest.IsPrimary
          }
        },
        correlationId: $"profile-{profileContext.ProfileId}");

    await messagePublisher.PublishAsync(message, cancellationToken);

    logger.LogDebug("Published ContactEditCommand for contact {ContactId} in profile {ProfileId}",
        editRequest.ContactId, profileContext.ProfileId);
  }

  /// <summary>
  /// Helper method to fire contact set primary command message
  /// </summary>
  private async Task FireContactSetPrimaryMessage(Guid contactId, ProfileContext profileContext, CancellationToken cancellationToken)
  {
    if (messagePublisher == null)
    {
      logger.LogError("Message publisher not available - cannot publish critical ContactSetPrimaryCommand for contact {ContactId}", contactId);
      throw new InvalidOperationException("Message publisher is required for Unity plugin operations");
    }

    var message = new PluginDataMessage(
        PluginId,
        "CONTACT_SET_PRIMARY_COMMAND",
        new
        {
          Action = "SetContactAsPrimary",
          ContactId = contactId,
          profileContext.ProfileId,
          profileContext.Provider
        },
        correlationId: $"profile-{profileContext.ProfileId}");

    await messagePublisher.PublishAsync(message, cancellationToken);

    logger.LogDebug("Published ContactSetPrimaryCommand for contact {ContactId} in profile {ProfileId}",
        contactId, profileContext.ProfileId);
  }

  /// <summary>
  /// Helper method to fire contact delete command message
  /// </summary>
  private async Task FireContactDeleteMessage(Guid contactId, ProfileContext profileContext, CancellationToken cancellationToken)
  {
    if (messagePublisher == null)
    {
      logger.LogError("Message publisher not available - cannot publish critical ContactDeleteCommand for contact {ContactId}", contactId);
      throw new InvalidOperationException("Message publisher is required for Unity plugin operations");
    }

    var message = new PluginDataMessage(
        PluginId,
        "CONTACT_DELETE_COMMAND",
        new
        {
          Action = "DeleteContact",
          ContactId = contactId,
          profileContext.ProfileId,
          profileContext.Provider
        },
        correlationId: $"profile-{profileContext.ProfileId}");

    await messagePublisher.PublishAsync(message, cancellationToken);

    logger.LogDebug("Published ContactDeleteCommand for contact {ContactId} in profile {ProfileId}",
        contactId, profileContext.ProfileId);
  }

  /// <summary>
  /// Optimistically updates the contacts cache with a new contact
  /// </summary>
  private async Task UpdateContactsCacheOptimistically(Guid contactId,
    CreateContactRequest contactRequest,
    ProfileContext profileContext,
    CancellationToken cancellationToken)
  {
    if (cacheInvalidationService == null)
    {
      logger.LogDebug("Cache invalidation service not available - skipping optimistic contact cache update");
      return;
    }

    try
    {
      // For now, we'll just invalidate the cache so it gets refreshed with the new data
      // In a more sophisticated implementation, we could actually update the cached data directly
      await cacheInvalidationService.InvalidateProfileDataCacheAsync(profileContext.ProfileId, PluginId, profileContext.Provider, "CONTACTS", cancellationToken);

      logger.LogDebug("Optimistically invalidated CONTACTS cache for new contact {ContactId} in ProfileId: {ProfileId}",
          contactId, profileContext.ProfileId);
    }
    catch (Exception ex)
    {
      logger.LogWarning(ex, "Failed to optimistically update CONTACTS cache for new contact {ContactId}", contactId);
      // Don't throw - optimistic cache updates are not critical to the main operation
    }
  }

  /// <summary>
  /// Optimistically updates the contacts cache with edited contact data
  /// </summary>
  private async Task UpdateContactsCacheOptimistically(EditContactRequest editRequest, ProfileContext profileContext, CancellationToken cancellationToken)
  {
    if (cacheInvalidationService == null)
    {
      logger.LogDebug("Cache invalidation service not available - skipping optimistic contact cache update");
      return;
    }

    try
    {
      // For now, we'll just invalidate the cache so it gets refreshed with the updated data
      // In a more sophisticated implementation, we could actually update the cached data directly
      await cacheInvalidationService.InvalidateProfileDataCacheAsync(profileContext.ProfileId, PluginId, profileContext.Provider, "CONTACTS", cancellationToken);

      logger.LogDebug("Optimistically invalidated CONTACTS cache for edited contact {ContactId} in ProfileId: {ProfileId}",
          editRequest.ContactId, profileContext.ProfileId);
    }
    catch (Exception ex)
    {
      logger.LogWarning(ex, "Failed to optimistically update CONTACTS cache for edited contact {ContactId}", editRequest.ContactId);
      // Don't throw - optimistic cache updates are not critical to the main operation
    }
  }

  /// <summary>
  /// Invalidate the CONTACTS cache for this profile/provider combination
  /// </summary>
  private async Task InvalidateContactsCache(Guid profileId, string provider, CancellationToken cancellationToken)
  {
    if (cacheInvalidationService == null)
    {
      logger.LogDebug("Cache invalidation service not available - skipping contacts cache invalidation");
      return;
    }

    try
    {
      await cacheInvalidationService.InvalidateProfileDataCacheAsync(profileId, PluginId, provider, "CONTACTS", cancellationToken);

      logger.LogDebug("Invalidated CONTACTS cache for ProfileId: {ProfileId}, PluginId: {PluginId}, Provider: {Provider}",
          profileId, PluginId, provider);
    }
    catch (Exception ex)
    {
      logger.LogWarning(ex, "Failed to invalidate CONTACTS cache for ProfileId: {ProfileId}", profileId);
      // Don't throw - cache invalidation failures shouldn't break the main operation
    }
  }
}
