using Ardalis.Result;
using Grants.ApplicantPortal.API.Core.DTOs;
using Grants.ApplicantPortal.API.Infrastructure.Messaging.Messages;

namespace Grants.ApplicantPortal.API.Plugins.Unity;

/// <summary>
/// Organization management implementation for Unity plugin
/// </summary>
public partial class UnityPlugin
{
  public async Task<Result> EditOrganizationAsync(
      EditOrganizationRequest editRequest,
      ProfileContext profileContext,
      CancellationToken cancellationToken = default)
  {
    logger.LogInformation("Unity plugin editing organization {OrganizationId} for ProfileId: {ProfileId}",
        editRequest.OrganizationId, profileContext.ProfileId);

    try
    {
      // 🔥 EVENT-DRIVEN: Publish edit command to outbox for Unity to process
      await FireOrganizationEditMessage(editRequest, profileContext, cancellationToken);

      // 🔥 Invalidate the ORGINFO cache when organization edit is queued
      await InvalidateOrganizationCache(profileContext.ProfileId, profileContext.Provider, cancellationToken);

      logger.LogInformation("Unity plugin queued organization edit - ID: {OrganizationId}, Name: {Name}, Type: {Type}",
          editRequest.OrganizationId, editRequest.Name, editRequest.OrganizationType);

      return Result.Success();
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Unity plugin failed to queue organization edit {OrganizationId} for ProfileId: {ProfileId}",
          editRequest.OrganizationId, profileContext.ProfileId);
      return Result.Error("Failed to queue organization edit for Unity system");
    }
  }

  /// <summary>
  /// Helper method to fire organization edit command message
  /// </summary>
  private async Task FireOrganizationEditMessage(EditOrganizationRequest editRequest, ProfileContext profileContext, CancellationToken cancellationToken)
  {
    if (messagePublisher == null)
    {
      logger.LogError("Message publisher not available - cannot publish critical OrganizationEditCommand for organization {OrganizationId}", editRequest.OrganizationId);
      throw new InvalidOperationException("Message publisher is required for Unity plugin operations");
    }

    var message = new PluginDataMessage(
        PluginId,
        "ORGANIZATION_EDIT_COMMAND",
        new
        {
          Action = "EditOrganization",
          editRequest.OrganizationId,
          profileContext.ProfileId,
          profileContext.Provider,
          Data = new
          {
            editRequest.Name,
            editRequest.OrganizationType,
            editRequest.OrganizationNumber,
            editRequest.Status,
            editRequest.NonRegOrgName,
            editRequest.FiscalMonth,
            editRequest.FiscalDay,
            editRequest.OrganizationSize
          }
        },
        correlationId: $"profile-{profileContext.ProfileId}");

    await messagePublisher.PublishAsync(message, cancellationToken);

    logger.LogDebug("Published OrganizationEditCommand for organization {OrganizationId} in profile {ProfileId}",
        editRequest.OrganizationId, profileContext.ProfileId);
  }

  /// <summary>
  /// Invalidate the ORGINFO cache for this profile/provider combination
  /// </summary>
  private async Task InvalidateOrganizationCache(Guid profileId, string provider, CancellationToken cancellationToken)
  {
    if (cacheInvalidationService == null)
    {
      logger.LogDebug("Cache invalidation service not available - skipping organization cache invalidation");
      return;
    }

    try
    {
      await cacheInvalidationService.InvalidateProfileDataCacheAsync(profileId, PluginId, provider, "ORGINFO", cancellationToken);

      logger.LogDebug("Invalidated ORGINFO cache for ProfileId: {ProfileId}, PluginId: {PluginId}, Provider: {Provider}",
          profileId, PluginId, provider);
    }
    catch (Exception ex)
    {
      logger.LogWarning(ex, "Failed to invalidate ORGINFO cache for ProfileId: {ProfileId}", profileId);
      // Don't throw - cache invalidation failures shouldn't break the main operation
    }
  }
}
