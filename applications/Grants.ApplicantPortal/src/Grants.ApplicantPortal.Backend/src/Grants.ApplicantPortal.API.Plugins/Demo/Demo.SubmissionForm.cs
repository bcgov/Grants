using Grants.ApplicantPortal.API.Core.DTOs;
using Grants.ApplicantPortal.API.Core.Plugins;
using Grants.ApplicantPortal.API.Plugins.Demo.Data;

namespace Grants.ApplicantPortal.API.Plugins.Demo;

/// <summary>
/// Submission form retrieval for the Demo plugin. Returns a form.io schema/data
/// fixture for a single submission, cached on-demand only when requested (never
/// eagerly seeded like the other profile scenarios).
/// </summary>
public partial class DemoPlugin
{
  internal const string SubmissionFormKey = "SUBMISSIONFORM";

  private async Task<ProfileData> PopulateSubmissionFormAsync(ProfilePopulationMetadata metadata, CancellationToken cancellationToken)
  {
    var submissionId = GetSubmissionId(metadata);
    var cacheSegment = $"{metadata.Provider}:{metadata.Key}:{submissionId}";

    return await pluginCacheService.GetOrFetchAsync<ProfileData>(
      metadata.ProfileId,
      PluginId,
      cacheSegment,
      _ =>
      {
        logger.LogInformation("Demo plugin generating submission form fixture for ProfileId: {ProfileId}, SubmissionId: {SubmissionId}",
            metadata.ProfileId, submissionId);

        var (schema, data) = SubmissionFormData.GetForm(submissionId);

        var profileData = new ProfileData(
            metadata.ProfileId,
            metadata.PluginId,
            metadata.Provider,
            metadata.Key,
            new SubmissionFormResponse(schema, data));

        return Task.FromResult(profileData);
      },
      cancellationToken: cancellationToken);
  }

  /// <summary>
  /// Extracts the submission id passed via <see cref="ProfilePopulationMetadata.AdditionalData"/>.
  /// </summary>
  private static string GetSubmissionId(ProfilePopulationMetadata metadata)
  {
    if (metadata.AdditionalData != null &&
        metadata.AdditionalData.TryGetValue("SubmissionId", out var value) &&
        value is not null)
    {
      return value.ToString() ?? string.Empty;
    }

    return string.Empty;
  }
}
