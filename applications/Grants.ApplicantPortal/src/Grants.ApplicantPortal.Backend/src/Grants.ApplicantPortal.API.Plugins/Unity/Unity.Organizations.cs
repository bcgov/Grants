using System.Text.Json;
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
      await UpdateOrganizationCacheOptimistically(editRequest, profileContext, cancellationToken);

      await FireOrganizationEditMessage(editRequest, profileContext, cancellationToken);

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

  // ── Fire messages ─────────────────────────────────────────────────────────

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
          profileContext.Subject,
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

  // ── Optimistic cache update ───────────────────────────────────────────────

  /// <summary>
  /// Optimistically patches the OrganizationInfo object in the cached ORGINFO data.
  /// The org data is a single object (not an array), so we use RebuildWithObject.
  /// </summary>
  private async Task UpdateOrganizationCacheOptimistically(EditOrganizationRequest editRequest,
    ProfileContext profileContext,
    CancellationToken cancellationToken)
  {
    await PatchCachedProfileDataAsync(
        profileContext.ProfileId, profileContext.Provider, "ORGINFO",
        root =>
        {
          // OrgInfo uses PascalCase property name "OrganizationInfo" from the API
          var propertyName = root.TryGetProperty("OrganizationInfo", out _) ? "OrganizationInfo" : "organizationInfo";

          return RebuildWithObject(root, propertyName, existing =>
          {
            return new
            {
              OrgName = editRequest.Name,
              OrgNumber = editRequest.OrganizationNumber,
              OrgStatus = editRequest.Status,
              editRequest.OrganizationType,
              editRequest.NonRegOrgName,
              OrgSize = editRequest.OrganizationSize?.ToString() ?? (existing.TryGetProperty("OrgSize", out var os)
                  ? os.GetString() : existing.TryGetProperty("orgSize", out var os2) ? os2.GetString() : null),
              FiscalMonth = editRequest.FiscalMonth ?? (existing.TryGetProperty("FiscalMonth", out var fm)
                  ? fm.GetString() : existing.TryGetProperty("fiscalMonth", out var fm2) ? fm2.GetString() : null),
              FiscalDay = editRequest.FiscalDay ?? (existing.TryGetProperty("FiscalDay", out var fd)
                  ? fd.GetInt32() : existing.TryGetProperty("fiscalDay", out var fd2) ? fd2.GetInt32() : (int?)null),
              OrganizationId = editRequest.OrganizationId.ToString(),
              LegalName = editRequest.LegalName ?? (existing.TryGetProperty("LegalName", out var ln)
                  ? ln.GetString() : existing.TryGetProperty("legalName", out var ln2) ? ln2.GetString() : null),
              DoingBusinessAs = existing.TryGetProperty("DoingBusinessAs", out var dba)
                  ? dba.GetString() : existing.TryGetProperty("doingBusinessAs", out var dba2) ? dba2.GetString() : null,
              LastUpdated = DateTime.UtcNow,
              AllowEdit = existing.TryGetProperty("AllowEdit", out var ae)
                  ? ae.GetBoolean() : existing.TryGetProperty("allowEdit", out var ae2) && ae2.GetBoolean()
            };
          });
        },
        cancellationToken);
  }
}
