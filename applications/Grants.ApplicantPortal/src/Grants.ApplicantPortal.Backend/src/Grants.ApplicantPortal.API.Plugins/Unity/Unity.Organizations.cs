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
  /// Optimistically patches the matching organization in the cached ORGINFO "organizations" array.
  /// The org data is an array (like contacts/addresses), so we use RebuildWithArray.
  /// </summary>
  private async Task UpdateOrganizationCacheOptimistically(EditOrganizationRequest editRequest,
    ProfileContext profileContext,
    CancellationToken cancellationToken)
  {
    var editId = editRequest.OrganizationId.ToString();

    await PatchCachedProfileDataAsync(
        profileContext.ProfileId, profileContext.Provider, "ORGINFO",
        root => RebuildWithArray(root, "organizations", (writer, arr) =>
        {
          foreach (var existing in arr.EnumerateArray())
          {
            if (existing.TryGetProperty("id", out var idProp) &&
                string.Equals(idProp.GetString(), editId, StringComparison.OrdinalIgnoreCase))
            {
              var updated = new
              {
                id = editId,
                orgName = editRequest.Name,
                organizationType = editRequest.OrganizationType,
                orgNumber = editRequest.OrganizationNumber,
                orgStatus = editRequest.Status.ToUpperInvariant(),
                nonRegOrgName = editRequest.NonRegOrgName,
                fiscalMonth = editRequest.FiscalMonth,
                fiscalDay = editRequest.FiscalDay,
                organizationSize = editRequest.OrganizationSize,
                sector = existing.TryGetProperty("sector", out var sec) ? sec.GetString() : null,
                subSector = existing.TryGetProperty("subSector", out var ss) ? ss.GetString() : null
              };
              JsonSerializer.Serialize(writer, updated, _camelCase);
            }
            else
            {
              existing.WriteTo(writer);
            }
          }
        }),
        cancellationToken);
  }
}
