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
        _logger.LogInformation("Unity plugin editing organization {OrganizationId} for ProfileId: {ProfileId}",
            editRequest.OrganizationId, profileContext.ProfileId);

        try
        {
            // 🔥 EVENT-DRIVEN: Publish edit command to outbox for Unity to process
            await FireOrganizationEditMessage(editRequest, profileContext, cancellationToken);

            // 🔥 Invalidate the ORGINFO cache when organization edit is queued
            await InvalidateOrganizationCache(profileContext.ProfileId, profileContext.Provider, cancellationToken);

            _logger.LogInformation("Unity plugin queued organization edit - ID: {OrganizationId}, Name: {Name}, Type: {Type}",
                editRequest.OrganizationId, editRequest.Name, editRequest.OrganizationType);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unity plugin failed to queue organization edit {OrganizationId} for ProfileId: {ProfileId}",
                editRequest.OrganizationId, profileContext.ProfileId);
            return Result.Error("Failed to queue organization edit for Unity system");
        }
    }

    /// <summary>
    /// Helper method to fire organization edit command message
    /// </summary>
    private async Task FireOrganizationEditMessage(EditOrganizationRequest editRequest, ProfileContext profileContext, CancellationToken cancellationToken)
    {
        if (_messagePublisher == null)
        {
            _logger.LogDebug("Message publisher not available - skipping organization edit message");
            return;
        }

        try
        {
            var message = new PluginDataMessage(
                PluginId,
                "ORGANIZATION_EDIT_COMMAND",
                new
                {
                    Action = "EditOrganization",
                    OrganizationId = editRequest.OrganizationId,
                    ProfileId = profileContext.ProfileId,
                    Provider = profileContext.Provider,
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

            await _messagePublisher.PublishAsync(message, cancellationToken);
            
            _logger.LogDebug("Published OrganizationEditCommand for organization {OrganizationId} in profile {ProfileId}", 
                editRequest.OrganizationId, profileContext.ProfileId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to publish OrganizationEditCommand for organization {OrganizationId}", editRequest.OrganizationId);
        }
    }

    /// <summary>
    /// Invalidate the ORGINFO cache for this profile/provider combination
    /// </summary>
    private async Task InvalidateOrganizationCache(Guid profileId, string provider, CancellationToken cancellationToken)
    {
        if (_cacheInvalidationService == null)
        {
            _logger.LogDebug("Cache invalidation service not available - skipping organization cache invalidation");
            return;
        }

        try
        {
            await _cacheInvalidationService.InvalidateProfileDataCacheAsync(profileId, PluginId, provider, "ORGINFO");
            
            _logger.LogDebug("Invalidated ORGINFO cache for ProfileId: {ProfileId}, PluginId: {PluginId}, Provider: {Provider}", 
                profileId, PluginId, provider);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to invalidate ORGINFO cache for ProfileId: {ProfileId}", profileId);
        }
    }
}
