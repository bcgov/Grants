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

  // ── Fire messages ─────────────────────────────────────────────────────────

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
            editRequest.IsPrimary
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

  // ── Optimistic cache updates ──────────────────────────────────────────────

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
                referenceNo = existing.TryGetProperty("referenceNo", out var rn) ? rn.GetString() : null
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
}
