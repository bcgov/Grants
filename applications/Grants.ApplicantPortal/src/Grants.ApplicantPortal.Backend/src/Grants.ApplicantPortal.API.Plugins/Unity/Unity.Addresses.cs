using System.Text.Json;
using Ardalis.Result;
using Grants.ApplicantPortal.API.Core.DTOs;
using Grants.ApplicantPortal.API.Infrastructure.Messaging.Messages;

namespace Grants.ApplicantPortal.API.Plugins.Unity;

/// <summary>
/// Address management implementation for Unity plugin
/// </summary>
public partial class UnityPlugin
{
  public async Task<Result<Guid>> CreateAddressAsync(
      CreateAddressRequest addressRequest,
      ProfileContext profileContext,
      CancellationToken cancellationToken = default)
  {
    logger.LogInformation("Unity plugin creating address for ProfileId: {ProfileId}, Type: {AddressType}",
        profileContext.ProfileId, addressRequest.AddressType);

    try
    {
      // Generate a new address ID for the Unity system
      var newAddressId = Guid.NewGuid();

      // 🔥 STEP 1: Update cache optimistically with the new address
      await UpdateAddressCacheOptimistically(newAddressId, addressRequest, profileContext, cancellationToken);

      // 🔥 STEP 2: Send command to Unity via message queue
      await FireAddressCreateMessage(newAddressId, addressRequest, profileContext, cancellationToken);

      logger.LogInformation("Unity plugin optimistically created address - ID: {AddressId}, Type: {AddressType}, Street: {Street}",
          newAddressId, addressRequest.AddressType, addressRequest.Street);

      return Result<Guid>.Success(newAddressId);
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Unity plugin failed to queue address creation for ProfileId: {ProfileId}, Type: {AddressType}",
          profileContext.ProfileId, addressRequest.AddressType);
      return Result<Guid>.Error("Failed to queue address creation for Unity system");
    }
  }

  public async Task<Result> EditAddressAsync(
      EditAddressRequest editRequest,
      ProfileContext profileContext,
      CancellationToken cancellationToken = default)
  {
    logger.LogInformation("Unity plugin editing address {AddressId} for ProfileId: {ProfileId}",
        editRequest.AddressId, profileContext.ProfileId);

    try
    {
      await UpdateAddressCacheOptimistically(editRequest, profileContext, cancellationToken);

      await FireAddressEditMessage(editRequest, profileContext, cancellationToken);

      logger.LogInformation("Unity plugin queued address edit - ID: {AddressId}, AddressType: {AddressType}, Street: {Street}",
          editRequest.AddressId, editRequest.AddressType, editRequest.Street);

      return Result.Success();
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Unity plugin failed to queue address edit {AddressId} for ProfileId: {ProfileId}",
          editRequest.AddressId, profileContext.ProfileId);

      return Result.Error("Failed to queue address edit for Unity system");
    }
  }

  public async Task<Result> SetAsPrimaryAddressAsync(
      Guid addressId,
      ProfileContext profileContext,
      CancellationToken cancellationToken = default)
  {
    logger.LogInformation("Unity plugin setting address {AddressId} as primary for ProfileId: {ProfileId}",
        addressId, profileContext.ProfileId);

    try
    {
      await UpdateAddressPrimaryCacheOptimistically(addressId, profileContext, cancellationToken);

      await FireAddressSetPrimaryMessage(addressId, profileContext, cancellationToken);

      logger.LogInformation("Unity plugin queued set address {AddressId} as primary for ProfileId: {ProfileId}",
          addressId, profileContext.ProfileId);

      return Result.Success();
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Unity plugin failed to queue set address {AddressId} as primary for ProfileId: {ProfileId}",
          addressId, profileContext.ProfileId);
      return Result.Error("Failed to queue set address as primary for Unity system");
    }
  }

  public async Task<Result> DeleteAddressAsync(
      Guid addressId,
      Guid applicantId,
      ProfileContext profileContext,
      CancellationToken cancellationToken = default)
  {
    logger.LogInformation("Unity plugin deleting address {AddressId} for ProfileId: {ProfileId}",
        addressId, profileContext.ProfileId);

    try
    {
      await DeleteAddressFromCacheOptimistically(addressId, profileContext, cancellationToken);

      await FireAddressDeleteMessage(addressId, applicantId, profileContext, cancellationToken);

      logger.LogInformation("Unity plugin queued address deletion {AddressId} for ProfileId: {ProfileId}",
          addressId, profileContext.ProfileId);

      return Result.Success();
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Unity plugin failed to queue address deletion {AddressId} for ProfileId: {ProfileId}",
          addressId, profileContext.ProfileId);
      return Result.Error("Failed to queue address deletion for Unity system");
    }
  }

  // ── Fire messages ─────────────────────────────────────────────────────────

  private async Task FireAddressCreateMessage(Guid addressId, CreateAddressRequest addressRequest, ProfileContext profileContext, CancellationToken cancellationToken)
  {
    if (messagePublisher == null)
    {
      logger.LogError("Message publisher not available - cannot publish critical AddressCreateCommand for address {AddressId}", addressId);
      throw new InvalidOperationException("Message publisher is required for Unity plugin operations");
    }

    var message = new PluginDataMessage(
        PluginId,
        "ADDRESS_CREATE_COMMAND",
        new
        {
          Action = "CreateAddress",
          AddressId = addressId,
          profileContext.ProfileId,
          profileContext.Provider,
          profileContext.Subject,
          Data = new
          {
            addressRequest.AddressType,
            addressRequest.Street,
            addressRequest.Street2,
            addressRequest.Unit,
            addressRequest.City,
            addressRequest.Province,
            addressRequest.PostalCode,
            addressRequest.Country,
            addressRequest.IsPrimary,
            addressRequest.ApplicantId
          }
        },
        correlationId: $"profile-{profileContext.ProfileId}");

    await messagePublisher.PublishAsync(message, cancellationToken);

    logger.LogDebug("Published AddressCreateCommand for address {AddressId} in profile {ProfileId}",
        addressId, profileContext.ProfileId);
  }

  private async Task FireAddressEditMessage(EditAddressRequest editRequest, ProfileContext profileContext, CancellationToken cancellationToken)
  {
    if (messagePublisher == null)
    {
      logger.LogError("Message publisher not available - cannot publish critical AddressEditCommand for address {AddressId}", editRequest.AddressId);
      throw new InvalidOperationException("Message publisher is required for Unity plugin operations");
    }

    var message = new PluginDataMessage(
        PluginId,
        "ADDRESS_EDIT_COMMAND",
        new
        {
          Action = "EditAddress",
          editRequest.AddressId,
          profileContext.ProfileId,
          profileContext.Provider,
          profileContext.Subject,
          Data = new
          {
            editRequest.AddressType,
            editRequest.Street,
            editRequest.Street2,
            editRequest.Unit,
            editRequest.City,
            editRequest.Province,
            editRequest.PostalCode,
            editRequest.Country,
            editRequest.IsPrimary,
            editRequest.ApplicantId
          }
        },
        correlationId: $"profile-{profileContext.ProfileId}");

    await messagePublisher.PublishAsync(message, cancellationToken);

    logger.LogDebug("Published AddressEditCommand for address {AddressId} in profile {ProfileId}",
        editRequest.AddressId, profileContext.ProfileId);
  }

  private async Task FireAddressSetPrimaryMessage(Guid addressId, ProfileContext profileContext, CancellationToken cancellationToken)
  {
    if (messagePublisher == null)
    {
      logger.LogError("Message publisher not available - cannot publish critical AddressSetPrimaryCommand for address {AddressId}", addressId);
      throw new InvalidOperationException("Message publisher is required for Unity plugin operations");
    }

    var message = new PluginDataMessage(
        PluginId,
        "ADDRESS_SET_PRIMARY_COMMAND",
        new
        {
          Action = "SetAddressAsPrimary",
          AddressId = addressId,
          profileContext.ProfileId,
          profileContext.Provider,
          profileContext.Subject
        },
        correlationId: $"profile-{profileContext.ProfileId}");

    await messagePublisher.PublishAsync(message, cancellationToken);

    logger.LogDebug("Published AddressSetPrimaryCommand for address {AddressId} in profile {ProfileId}",
        addressId, profileContext.ProfileId);
  }

  /// <summary>
  /// Helper method to fire address delete command message
  /// </summary>
  private async Task FireAddressDeleteMessage(Guid addressId,
    Guid applicantId,
    ProfileContext profileContext, CancellationToken cancellationToken)
  {
    if (messagePublisher == null)
    {
      logger.LogError("Message publisher not available - cannot publish critical AddressDeleteCommand for address {AddressId}", addressId);
      throw new InvalidOperationException("Message publisher is required for Unity plugin operations");
    }

    var message = new PluginDataMessage(
        PluginId,
        "ADDRESS_DELETE_COMMAND",
        new
        {
          Action = "DeleteAddress",
          AddressId = addressId,
          profileContext.ProfileId,
          profileContext.Provider,
          profileContext.Subject,
          Data = new
          {
            ApplicantId = applicantId
          },
        },
        correlationId: $"profile-{profileContext.ProfileId}");

    await messagePublisher.PublishAsync(message, cancellationToken);

    logger.LogDebug("Published AddressDeleteCommand for address {AddressId} in profile {ProfileId}",
        addressId, profileContext.ProfileId);
  }

  // ── Optimistic cache updates ──────────────────────────────────────────────

  /// <summary>
  /// Optimistically appends the new address to the cached addresses array.
  /// </summary>
  private async Task UpdateAddressCacheOptimistically(Guid addressId,
    CreateAddressRequest addressRequest,
    ProfileContext profileContext,
    CancellationToken cancellationToken)
  {
    var newAddress = new
    {
      id = addressId.ToString(),
      addressRequest.AddressType,
      addressRequest.Street,
      street2 = addressRequest.Street2 ?? "",
      unit = addressRequest.Unit ?? "",
      addressRequest.City,
      addressRequest.Province,
      addressRequest.PostalCode,
      country = addressRequest.Country ?? "",
      addressRequest.IsPrimary,
      isEditable = true,
      referenceNo = (string?)null,
      creationTime = DateTimeOffset.UtcNow
    };

    await PatchCachedProfileDataAsync(
        profileContext.ProfileId, profileContext.Provider, "ADDRESSINFO",
        root => RebuildWithArray(root, "addresses", (writer, arr) =>
        {
          foreach (var existing in arr.EnumerateArray())
          {
            // If the new address is primary, clear isPrimary on all existing addresses
            if (addressRequest.IsPrimary && existing.TryGetProperty("isPrimary", out _))
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
          JsonSerializer.Serialize(writer, newAddress, _camelCase);
        }),
        cancellationToken);
  }

  /// <summary>
  /// Optimistically replaces the edited address in the cached addresses array.
  /// When the edited address has IsPrimary set to true, other addresses are toggled to not primary.
  /// </summary>
  private async Task UpdateAddressCacheOptimistically(EditAddressRequest editRequest,
    ProfileContext profileContext,
    CancellationToken cancellationToken)
  {
    var editId = editRequest.AddressId.ToString();

    await PatchCachedProfileDataAsync(
        profileContext.ProfileId, profileContext.Provider, "ADDRESSINFO",
        root => RebuildWithArray(root, "addresses", (writer, arr) =>
        {
          foreach (var existing in arr.EnumerateArray())
          {
            if (existing.TryGetProperty("id", out var idProp) &&
                string.Equals(idProp.GetString(), editId, StringComparison.OrdinalIgnoreCase))
            {
              var updated = new
              {
                id = editId,
                editRequest.AddressType,
                editRequest.Street,
                street2 = editRequest.Street2 ?? "",
                unit = editRequest.Unit ?? "",
                editRequest.City,
                editRequest.Province,
                editRequest.PostalCode,
                country = editRequest.Country ?? "",
                editRequest.IsPrimary,
                isEditable = existing.TryGetProperty("isEditable", out var ed) && ed.GetBoolean(),
                referenceNo = existing.TryGetProperty("referenceNo", out var rn) ? rn.GetString() : null,
                creationTime = existing.TryGetProperty("creationTime", out var ct) ? ct.GetString() : null
              };
              JsonSerializer.Serialize(writer, updated, _camelCase);
            }
            else if (editRequest.IsPrimary)
            {
              // Toggle other addresses to not primary when the edited address becomes primary
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
  /// Optimistically toggles isPrimary flags in the cached addresses array.
  /// </summary>
  private async Task UpdateAddressPrimaryCacheOptimistically(Guid addressId,
    ProfileContext profileContext,
    CancellationToken cancellationToken)
  {
    var targetId = addressId.ToString();

    await PatchCachedProfileDataAsync(
        profileContext.ProfileId, profileContext.Provider, "ADDRESSINFO",
        root => RebuildWithArray(root, "addresses", (writer, arr) =>
        {
          foreach (var existing in arr.EnumerateArray())
          {
            var isTarget = existing.TryGetProperty("id", out var idProp) &&
                           string.Equals(idProp.GetString(), targetId, StringComparison.OrdinalIgnoreCase);

            writer.WriteStartObject();
            foreach (var prop in existing.EnumerateObject())
            {
              if (prop.NameEquals("isPrimary"))
                writer.WriteBoolean("isPrimary", isTarget);
              else
                prop.WriteTo(writer);
            }
            writer.WriteEndObject();
          }
        }),
        cancellationToken);
  }

  /// <summary>
  /// Optimistically removes the address from the cached addresses array.
  /// If the deleted address was primary, auto-promotes the most recent remaining address.
  /// </summary>
  private async Task DeleteAddressFromCacheOptimistically(Guid addressId,
    ProfileContext profileContext,
    CancellationToken cancellationToken)
  {
    var targetId = addressId.ToString();

    await PatchCachedProfileDataAsync(
        profileContext.ProfileId, profileContext.Provider, "ADDRESSINFO",
        root => RebuildWithArray(root, "addresses", (writer, arr) =>
        {
          // First pass: determine if the deleted address was primary
          var deletedWasPrimary = false;
          foreach (var a in arr.EnumerateArray())
          {
            if (a.TryGetProperty("id", out var id) &&
                string.Equals(id.GetString(), targetId, StringComparison.OrdinalIgnoreCase) &&
                a.TryGetProperty("isPrimary", out var ip) && ip.GetBoolean())
            {
              deletedWasPrimary = true;
              break;
            }
          }

          // Collect remaining addresses (excluding the deleted one)
          var remaining = new List<JsonElement>();
          foreach (var a in arr.EnumerateArray())
          {
            if (a.TryGetProperty("id", out var id) &&
                string.Equals(id.GetString(), targetId, StringComparison.OrdinalIgnoreCase))
              continue;
            remaining.Add(a.Clone());
          }

          // If the deleted address was primary, promote the most recently created remaining address
          var promotedId = (string?)null;
          if (deletedWasPrimary && remaining.Count > 0)
          {
            JsonElement? best = null;
            DateTimeOffset bestTime = DateTimeOffset.MinValue;

            foreach (var r in remaining)
            {
              if (r.TryGetProperty("creationTime", out var ctProp) &&
                  DateTimeOffset.TryParse(ctProp.GetString(), out var ct) &&
                  ct > bestTime)
              {
                bestTime = ct;
                best = r;
              }
            }

            // Fall back to the first remaining address if none have a creationTime
            var candidate = best ?? remaining[0];
            if (candidate.TryGetProperty("id", out var cid))
              promotedId = cid.GetString();
          }

          // Write remaining addresses, promoting one if needed
          foreach (var a in remaining)
          {
            if (promotedId != null)
            {
              var isPromoted = a.TryGetProperty("id", out var cid) &&
                               string.Equals(cid.GetString(), promotedId, StringComparison.OrdinalIgnoreCase);

              writer.WriteStartObject();
              foreach (var prop in a.EnumerateObject())
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
              a.WriteTo(writer);
            }
          }
        }),
        cancellationToken);
  }
}
