using System.Text.Json;
using System.Globalization;
using Ardalis.Result;
using Grants.ApplicantPortal.API.Core.DTOs;
using Grants.ApplicantPortal.API.Core.Plugins;
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

      // A type group with no flagged primary has one inferred on read. Persist that decision
      // instead of leaving it to be recomputed: otherwise the next address added to the same
      // group is newer, wins the inference, and silently takes the primary slot.
      addressRequest = await PromoteWhenTypeGroupHasNoPrimary(addressRequest, profileContext, cancellationToken);

      // 🔥 STEP 1: Update cache optimistically with the new address
      await UpdateAddressCacheOptimistically(newAddressId, addressRequest, profileContext, cancellationToken);

      // 🔥 STEP 2: Send command to Unity via message queue — carries the same IsPrimary the
      // cache was patched with, so the flag survives the next hydration from Unity.
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
  /// When the new address is primary, only the existing addresses of the SAME address type
  /// are demoted — every other address type keeps its own primary.
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
            // If the new address is primary, clear isPrimary on the existing addresses of the same type only.
            // The flag is written even when the cached entry does not carry it yet, so a stale
            // entry can never remain primary within the type group.
            if (addressRequest.IsPrimary && IsSameAddressType(existing, addressRequest.AddressType))
            {
              WriteAddressWithPrimary(writer, existing, false);
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
  /// When the edited address has IsPrimary set to true, only the other addresses of the
  /// edited address's NEW type are toggled to not primary — other types keep their own primary.
  /// When the edit also changes the address type, the address leaves its old type group behind;
  /// that group then has no flagged primary and one is inferred on the next read.
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
          // Read the address type the edited address had BEFORE the overwrite, so a type
          // change can be detected and the old type group can be left to re-infer its primary.
          var previousAddressType = FindAddressTypeById(arr, editId);
          var typeChanged = previousAddressType != null &&
                            !string.Equals(previousAddressType, editRequest.AddressType, StringComparison.OrdinalIgnoreCase);

          if (typeChanged)
          {
            logger.LogDebug(
                "Address {AddressId} changed type from {PreviousAddressType} to {AddressType} — the previous type group will re-infer its primary",
                editId, previousAddressType, editRequest.AddressType);
          }

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
            else if (editRequest.IsPrimary && IsSameAddressType(existing, editRequest.AddressType))
            {
              // Toggle the other addresses of the same type to not primary when the edited
              // address becomes primary. Addresses of any other type are left untouched.
              WriteAddressWithPrimary(writer, existing, false);
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
  /// The address type is derived from the target address itself (it is an attribute of the
  /// address, not a caller-supplied parameter) and only that type group is re-flagged.
  /// Addresses of every other type are written back untouched.
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
          // First pass: determine the target address's own type — that is the group to re-flag.
          var targetAddressType = FindAddressTypeById(arr, targetId);

          foreach (var existing in arr.EnumerateArray())
          {
            var isTarget = existing.TryGetProperty("id", out var idProp) &&
                           string.Equals(idProp.GetString(), targetId, StringComparison.OrdinalIgnoreCase);

            // Only the target's type group is rewritten; other groups stay byte-identical.
            if (isTarget || (targetAddressType != null && IsSameAddressType(existing, targetAddressType)))
            {
              WriteAddressWithPrimary(writer, existing, isTarget);
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
  /// Optimistically removes the address from the cached addresses array.
  /// If the deleted address was primary, auto-promotes the most recent remaining address
  /// OF THE SAME ADDRESS TYPE. Addresses of every other type are left untouched.
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
          // First pass: determine if the deleted address was primary, and which type group it belonged to
          var deletedWasPrimary = false;
          string? deletedAddressType = null;
          foreach (var a in arr.EnumerateArray())
          {
            if (a.TryGetProperty("id", out var id) &&
                string.Equals(id.GetString(), targetId, StringComparison.OrdinalIgnoreCase))
            {
              deletedAddressType = ReadAddressTypeValue(a);
              deletedWasPrimary = a.TryGetProperty("isPrimary", out var ip) && ip.ValueKind == JsonValueKind.True;
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

          // Only the deleted address's own type group can lose its primary, so promotion is
          // restricted to the remaining addresses of that type.
          var sameTypeRemaining = deletedAddressType == null
            ? []
            : remaining.Where(r => IsSameAddressType(r, deletedAddressType)).ToList();

          // If the deleted address was primary, promote the most recently created remaining address of that type
          var promotedId = (string?)null;
          if (deletedWasPrimary && sameTypeRemaining.Count > 0)
          {
            JsonElement? best = null;
            DateTimeOffset bestTime = DateTimeOffset.MinValue;

            foreach (var r in sameTypeRemaining)
            {
              if (r.TryGetProperty("creationTime", out var ctProp) &&
                  ctProp.ValueKind == JsonValueKind.String &&
                  DateTimeOffset.TryParse(
                      ctProp.GetString(),
                      CultureInfo.InvariantCulture,
                      DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                      out var ct) &&
                  ct > bestTime)
              {
                bestTime = ct;
                best = r;
              }
            }

            // Fall back to the first remaining address of that type if none have a creationTime
            var candidate = best ?? sameTypeRemaining[0];
            if (candidate.TryGetProperty("id", out var cid))
              promotedId = cid.GetString();
          }

          // Write remaining addresses, promoting one within the deleted address's type group if needed
          foreach (var a in remaining)
          {
            if (promotedId != null && IsSameAddressType(a, deletedAddressType))
            {
              var isPromoted = a.TryGetProperty("id", out var cid) &&
                               string.Equals(cid.GetString(), promotedId, StringComparison.OrdinalIgnoreCase);

              WriteAddressWithPrimary(writer, a, isPromoted);
            }
            else
            {
              a.WriteTo(writer);
            }
          }
        }),
        cancellationToken);
  }

  // ── Address type helpers ──────────────────────────────────────────────────

  /// <summary>
  /// Reads a cached address's type. A missing, non-string or blank value is normalized to an
  /// empty string so that untyped addresses still form one consistent group.
  /// </summary>
  private static string ReadAddressTypeValue(JsonElement address)
  {
    if (address.ValueKind == JsonValueKind.Object &&
        address.TryGetProperty("addressType", out var typeProp) &&
        typeProp.ValueKind == JsonValueKind.String)
    {
      return AddressTypeKey.Normalize(typeProp.GetString());
    }

    return AddressTypeKey.Unknown;
  }

  /// <summary>
  /// Returns true when the cached address belongs to the given address type group.
  /// Types are compared case-insensitively; no type value is ever hardcoded.
  /// </summary>
  private static bool IsSameAddressType(JsonElement address, string? addressType)
    => AddressTypeKey.AreSame(ReadAddressTypeValue(address), addressType);

  /// <summary>
  /// Marks a new address as primary when no address of its type is flagged yet, so the primary
  /// the reader would infer anyway is written down. Requests that already ask for primary, and
  /// groups that already have one, are returned untouched.
  /// </summary>
  private async Task<CreateAddressRequest> PromoteWhenTypeGroupHasNoPrimary(
      CreateAddressRequest addressRequest,
      ProfileContext profileContext,
      CancellationToken cancellationToken)
  {
    if (addressRequest.IsPrimary || await TypeGroupHasFlaggedPrimary(addressRequest.AddressType, profileContext, cancellationToken))
    {
      return addressRequest;
    }

    logger.LogInformation(
        "No primary address exists for type {AddressType} on ProfileId {ProfileId} — the new address is created as that type's primary",
        addressRequest.AddressType, profileContext.ProfileId);

    return addressRequest with { IsPrimary = true };
  }

  /// <summary>
  /// Reads the cache and reports whether any address of the given type already carries an
  /// explicit isPrimary flag. Unflagged addresses do not count: a group whose primary is only
  /// inferred is exactly the case that needs the flag written down.
  /// </summary>
  private async Task<bool> TypeGroupHasFlaggedPrimary(
      string addressType,
      ProfileContext profileContext,
      CancellationToken cancellationToken)
  {
    var cached = await pluginCacheService.TryGetAsync<ProfileData>(
        profileContext.ProfileId, PluginId, $"{profileContext.Provider}:ADDRESSINFO", cancellationToken);

    if (cached?.Data is not JsonElement root ||
        !root.TryGetProperty("addresses", out var addresses) ||
        addresses.ValueKind != JsonValueKind.Array)
    {
      return false;
    }

    foreach (var address in addresses.EnumerateArray())
    {
      if (IsSameAddressType(address, addressType) &&
          address.TryGetProperty("isPrimary", out var isPrimary) &&
          isPrimary.ValueKind == JsonValueKind.True)
      {
        return true;
      }
    }

    return false;
  }

  /// <summary>
  /// Finds the address type of the address with the given ID, or null when it is not cached.
  /// </summary>
  private static string? FindAddressTypeById(JsonElement addresses, string addressId)
  {
    foreach (var address in addresses.EnumerateArray())
    {
      if (address.TryGetProperty("id", out var idProp) &&
          string.Equals(idProp.GetString(), addressId, StringComparison.OrdinalIgnoreCase))
      {
        return ReadAddressTypeValue(address);
      }
    }

    return null;
  }

  /// <summary>
  /// Writes a cached address back with the given isPrimary value. The flag is written even
  /// when the source entry does not carry the property, so no entry can silently stay primary.
  /// </summary>
  private static void WriteAddressWithPrimary(Utf8JsonWriter writer, JsonElement address, bool isPrimary)
  {
    writer.WriteStartObject();

    var wroteIsPrimary = false;
    foreach (var prop in address.EnumerateObject())
    {
      if (prop.NameEquals("isPrimary"))
      {
        writer.WriteBoolean("isPrimary", isPrimary);
        wroteIsPrimary = true;
      }
      else
      {
        prop.WriteTo(writer);
      }
    }

    if (!wroteIsPrimary)
      writer.WriteBoolean("isPrimary", isPrimary);

    writer.WriteEndObject();
  }
}
