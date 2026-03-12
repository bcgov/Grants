using System.Text.Json;
using Ardalis.Result;
using Grants.ApplicantPortal.API.Core.DTOs;
using Grants.ApplicantPortal.API.Core.Plugins;
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
        profileContext.ProfileId, contactRequest.Name, contactRequest.ContactType);

    try
    {
      // Generate a new contact ID for the Unity system
      var newContactId = Guid.NewGuid();

      // 🔥 STEP 1: Update cache optimistically with the new contact
      await UpdateContactsCacheOptimistically(newContactId, contactRequest, profileContext, cancellationToken);

      // 🔥 STEP 2: Send command to Unity via message queue
      await FireContactCreateMessage(newContactId, contactRequest, profileContext, cancellationToken);

      logger.LogInformation("Unity plugin optimistically created contact - ID: {ContactId}, Name: {Name}, Type: {Type}, Email: {Email}",
          newContactId, contactRequest.Name, contactRequest.ContactType, contactRequest.Email);

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
          editRequest.ContactId, editRequest.Name, editRequest.ContactType);

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
      await UpdateContactsPrimaryCacheOptimistically(contactId, profileContext, cancellationToken);

      await FireContactSetPrimaryMessage(contactId, profileContext, cancellationToken);

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
      await DeleteContactFromCacheOptimistically(contactId, profileContext, cancellationToken);

      await FireContactDeleteMessage(contactId, profileContext, cancellationToken);

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
          profileContext.Subject,
          Data = new
          {
            contactRequest.Name,
            contactRequest.Email,
            contactRequest.Title,
            contactRequest.ContactType,
            contactRequest.HomePhoneNumber,
            contactRequest.MobilePhoneNumber,
            contactRequest.WorkPhoneNumber,
            contactRequest.WorkPhoneExtension,
            contactRequest.Role,
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
          profileContext.Subject,
          Data = new
          {
            editRequest.Name,
            editRequest.Email,
            editRequest.Title,
            editRequest.ContactType,
            editRequest.HomePhoneNumber,
            editRequest.MobilePhoneNumber,
            editRequest.WorkPhoneNumber,
            editRequest.WorkPhoneExtension,
            editRequest.Role,
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
          profileContext.Provider,
          profileContext.Subject
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
          profileContext.Provider,
          profileContext.Subject
        },
        correlationId: $"profile-{profileContext.ProfileId}");

    await messagePublisher.PublishAsync(message, cancellationToken);

    logger.LogDebug("Published ContactDeleteCommand for contact {ContactId} in profile {ProfileId}",
        contactId, profileContext.ProfileId);
  }

  /// <summary>
  /// Optimistically appends the new contact to the cached contacts array.
  /// </summary>
  private async Task UpdateContactsCacheOptimistically(Guid contactId,
    CreateContactRequest contactRequest,
    ProfileContext profileContext,
    CancellationToken cancellationToken)
  {
    var newContact = new
    {
      contactId = contactId.ToString(),
      contactRequest.Name,
      contactRequest.Title,
      contactRequest.Email,
      contactRequest.HomePhoneNumber,
      contactRequest.MobilePhoneNumber,
      contactRequest.WorkPhoneNumber,
      contactRequest.WorkPhoneExtension,
      contactRequest.ContactType,
      contactRequest.Role,
      contactRequest.IsPrimary,
      isEditable = true,
      applicationId = (string?)null
    };

    await PatchCachedProfileDataAsync(
        profileContext.ProfileId, profileContext.Provider, "CONTACTINFO",
        root => RebuildWithArray(root, "contacts", (writer, arr) =>
        {
          foreach (var existing in arr.EnumerateArray())
          {
            // If the new contact is primary, clear isPrimary on all existing contacts
            if (contactRequest.IsPrimary && existing.TryGetProperty("isPrimary", out _))
            {
              writer.WriteStartObject();
              foreach (var prop in existing.EnumerateObject())
              {
                if (prop.NameEquals("isPrimary"))
                  writer.WriteBoolean("isPrimary", false);
                else
                  prop.WriteTo(writer);
              }
              writer.WriteEndObject();
            }
            else
            {
              existing.WriteTo(writer);
            }
          }
          JsonSerializer.Serialize(writer, newContact, _camelCase);
        }),
        cancellationToken);
  }

  /// <summary>
  /// Optimistically replaces the edited contact in the cached contacts array.
  /// </summary>
  private async Task UpdateContactsCacheOptimistically(EditContactRequest editRequest,
    ProfileContext profileContext,
    CancellationToken cancellationToken)
  {
    var editId = editRequest.ContactId.ToString();

    await PatchCachedProfileDataAsync(
        profileContext.ProfileId, profileContext.Provider, "CONTACTINFO",
        root => RebuildWithArray(root, "contacts", (writer, arr) =>
        {
          foreach (var existing in arr.EnumerateArray())
          {
            if (existing.TryGetProperty("contactId", out var idProp) &&
                string.Equals(idProp.GetString(), editId, StringComparison.OrdinalIgnoreCase))
            {
              var updated = new
              {
                contactId = editId,
                editRequest.Name,
                editRequest.Title,
                editRequest.Email,
                editRequest.HomePhoneNumber,
                editRequest.MobilePhoneNumber,
                editRequest.WorkPhoneNumber,
                editRequest.WorkPhoneExtension,
                editRequest.ContactType,
                editRequest.Role,
                editRequest.IsPrimary,
                isEditable = existing.TryGetProperty("isEditable", out var ed) && ed.GetBoolean(),
                applicationId = existing.TryGetProperty("applicationId", out var aid) ? aid.GetString() : null
              };
              JsonSerializer.Serialize(writer, updated, _camelCase);
            }
            else if (editRequest.IsPrimary)
            {
              // Clear isPrimary on all other contacts when edited contact becomes primary
              writer.WriteStartObject();
              foreach (var prop in existing.EnumerateObject())
              {
                if (prop.NameEquals("isPrimary"))
                  writer.WriteBoolean("isPrimary", false);
                else
                  prop.WriteTo(writer);
              }
              writer.WriteEndObject();
            }
            else
            {
              existing.WriteTo(writer);
            }
          }
        }),
        cancellationToken);
  }

  /// <summary>
  /// Optimistically toggles isPrimary flags in the cached contacts array.
  /// </summary>
  private async Task UpdateContactsPrimaryCacheOptimistically(Guid contactId,
    ProfileContext profileContext,
    CancellationToken cancellationToken)
  {
    var targetId = contactId.ToString();

    await PatchCachedProfileDataAsync(
        profileContext.ProfileId, profileContext.Provider, "CONTACTINFO",
        root => RebuildWithArray(root, "contacts", (writer, arr) =>
        {
          foreach (var existing in arr.EnumerateArray())
          {
            var isTarget = existing.TryGetProperty("contactId", out var idProp) &&
                           string.Equals(idProp.GetString(), targetId, StringComparison.OrdinalIgnoreCase);

            // Rewrite the contact with isPrimary toggled
            writer.WriteStartObject();
            foreach (var prop in existing.EnumerateObject())
            {
              if (prop.NameEquals("isPrimary"))
              {
                writer.WriteBoolean("isPrimary", isTarget);
              }
              else
              {
                prop.WriteTo(writer);
              }
            }
            writer.WriteEndObject();
          }
        }),
        cancellationToken);
  }

  /// <summary>
  /// Optimistically removes the contact from the cached contacts array.
  /// If the deleted contact was primary, auto-promotes the most recent remaining contact.
  /// </summary>
  private async Task DeleteContactFromCacheOptimistically(Guid contactId,
    ProfileContext profileContext,
    CancellationToken cancellationToken)
  {
    var targetId = contactId.ToString();

    await PatchCachedProfileDataAsync(
        profileContext.ProfileId, profileContext.Provider, "CONTACTINFO",
        root => RebuildWithArray(root, "contacts", (writer, arr) =>
        {
          // First pass: determine if the deleted contact was primary
          var deletedWasPrimary = false;
          foreach (var c in arr.EnumerateArray())
          {
            if (c.TryGetProperty("contactId", out var id) &&
                string.Equals(id.GetString(), targetId, StringComparison.OrdinalIgnoreCase) &&
                c.TryGetProperty("isPrimary", out var ip) && ip.GetBoolean())
            {
              deletedWasPrimary = true;
              break;
            }
          }

          // Collect remaining contacts (excluding the deleted one)
          var remaining = new List<JsonElement>();
          foreach (var c in arr.EnumerateArray())
          {
            if (c.TryGetProperty("contactId", out var id) &&
                string.Equals(id.GetString(), targetId, StringComparison.OrdinalIgnoreCase))
              continue;
            remaining.Add(c.Clone());
          }

          // If the deleted contact was primary, promote the first remaining contact
          var promotedId = (string?)null;
          if (deletedWasPrimary && remaining.Count > 0)
          {
            var candidate = remaining[0];
            if (candidate.TryGetProperty("contactId", out var cid))
              promotedId = cid.GetString();
          }

          // Write remaining contacts, promoting one if needed
          foreach (var c in remaining)
          {
            if (promotedId != null)
            {
              var isPromoted = c.TryGetProperty("contactId", out var cid) &&
                               string.Equals(cid.GetString(), promotedId, StringComparison.OrdinalIgnoreCase);

              writer.WriteStartObject();
              foreach (var prop in c.EnumerateObject())
              {
                if (prop.NameEquals("isPrimary"))
                  writer.WriteBoolean("isPrimary", isPromoted);
                else
                  prop.WriteTo(writer);
              }
              writer.WriteEndObject();
            }
            else
            {
              c.WriteTo(writer);
            }
          }
        }),
        cancellationToken);
  }
}
