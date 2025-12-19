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
    _logger.LogInformation("Unity plugin editing address {AddressId} for ProfileId: {ProfileId}",
        editRequest.AddressId, profileContext.ProfileId);

    try
    {
      // 🔥 EVENT-DRIVEN: Publish edit command to outbox for Unity to process
      await FireAddressEditMessage(editRequest, profileContext, cancellationToken);

      // 🔥 Invalidate the ADDRESSES cache when address edit is queued
      await InvalidateAddressesCache(profileContext.ProfileId, profileContext.Provider, cancellationToken);

      _logger.LogInformation("Unity plugin queued address edit - ID: {AddressId}, Type: {Type}, Address: {Address}",
          editRequest.AddressId, editRequest.Type, editRequest.Address);

      return Result.Success();
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Unity plugin failed to queue address edit {AddressId} for ProfileId: {ProfileId}",
          editRequest.AddressId, profileContext.ProfileId);
      return Result.Error("Failed to queue address edit for Unity system");
    }
  }

  public async Task<Result> SetAsPrimaryAddressAsync(
      Guid addressId,
      ProfileContext profileContext,
      CancellationToken cancellationToken = default)
  {
    _logger.LogInformation("Unity plugin setting address {AddressId} as primary for ProfileId: {ProfileId}",
        addressId, profileContext.ProfileId);

    try
    {
      // 🔥 EVENT-DRIVEN: Publish set primary command to outbox for Unity to process
      await FireAddressSetPrimaryMessage(addressId, profileContext, cancellationToken);

      // 🔥 Invalidate the ADDRESSES cache when primary address change is queued
      await InvalidateAddressesCache(profileContext.ProfileId, profileContext.Provider, cancellationToken);

      _logger.LogInformation("Unity plugin queued set address {AddressId} as primary for ProfileId: {ProfileId}",
          addressId, profileContext.ProfileId);

      return Result.Success();
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Unity plugin failed to queue set address {AddressId} as primary for ProfileId: {ProfileId}",
          addressId, profileContext.ProfileId);
      return Result.Error("Failed to queue set address as primary for Unity system");
    }
  }

  /// <summary>
  /// Helper method to fire address edit command message
  /// </summary>
  private async Task FireAddressEditMessage(EditAddressRequest editRequest, ProfileContext profileContext, CancellationToken cancellationToken)
  {
    if (_messagePublisher == null)
    {
      _logger.LogDebug("Message publisher not available - skipping address edit message");
      return;
    }

    try
    {
      var message = new PluginDataMessage(
          PluginId,
          "ADDRESS_EDIT_COMMAND",
          new
          {
            Action = "EditAddress",
            AddressId = editRequest.AddressId,
            ProfileId = profileContext.ProfileId,
            Provider = profileContext.Provider,
            Data = new
            {
              editRequest.Type,
              editRequest.Address,
              editRequest.City,
              editRequest.Province,
              editRequest.PostalCode,
              editRequest.Country,
              editRequest.IsPrimary
            }
          },
          correlationId: $"profile-{profileContext.ProfileId}");

      await _messagePublisher.PublishAsync(message, cancellationToken);

      _logger.LogDebug("Published AddressEditCommand for address {AddressId} in profile {ProfileId}",
          editRequest.AddressId, profileContext.ProfileId);
    }
    catch (Exception ex)
    {
      _logger.LogWarning(ex, "Failed to publish AddressEditCommand for address {AddressId}", editRequest.AddressId);
    }
  }

  /// <summary>
  /// Helper method to fire address set primary command message
  /// </summary>
  private async Task FireAddressSetPrimaryMessage(Guid addressId, ProfileContext profileContext, CancellationToken cancellationToken)
  {
    if (_messagePublisher == null)
    {
      _logger.LogDebug("Message publisher not available - skipping address set primary message");
      return;
    }

    try
    {
      var message = new PluginDataMessage(
          PluginId,
          "ADDRESS_SET_PRIMARY_COMMAND",
          new
          {
            Action = "SetAddressAsPrimary",
            AddressId = addressId,
            ProfileId = profileContext.ProfileId,
            Provider = profileContext.Provider
          },
          correlationId: $"profile-{profileContext.ProfileId}");

      await _messagePublisher.PublishAsync(message, cancellationToken);

      _logger.LogDebug("Published AddressSetPrimaryCommand for address {AddressId} in profile {ProfileId}",
          addressId, profileContext.ProfileId);
    }
    catch (Exception ex)
    {
      _logger.LogWarning(ex, "Failed to publish AddressSetPrimaryCommand for address {AddressId}", addressId);
    }
  }

  /// <summary>
  /// Invalidate the ADDRESSES cache for this profile/provider combination
  /// </summary>
  private async Task InvalidateAddressesCache(Guid profileId, string provider, CancellationToken cancellationToken)
  {
    if (_cacheInvalidationService == null)
    {
      _logger.LogDebug("Cache invalidation service not available - skipping addresses cache invalidation");
      return;
    }

    try
    {
      await _cacheInvalidationService.InvalidateProfileDataCacheAsync(profileId,
        PluginId,
        provider,
        "ADDRESSES",
        cancellationToken);

      _logger.LogDebug("Invalidated ADDRESSES cache for ProfileId: {ProfileId}, PluginId: {PluginId}, Provider: {Provider}",
          profileId, PluginId, provider);
    }
    catch (Exception ex)
    {
      _logger.LogWarning(ex, "Failed to invalidate ADDRESSES cache for ProfileId: {ProfileId}", profileId);
    }
  }
}
