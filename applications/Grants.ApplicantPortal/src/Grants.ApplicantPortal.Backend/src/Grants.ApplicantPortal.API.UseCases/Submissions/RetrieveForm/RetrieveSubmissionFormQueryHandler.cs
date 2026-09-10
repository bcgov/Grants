using System.Text.Json;
using Grants.ApplicantPortal.API.Core.Plugins;

namespace Grants.ApplicantPortal.API.UseCases.Submissions.RetrieveForm;

/// <summary>
/// Query handler for retrieving a single submission's form.io schema and data,
/// with automatic cache hydration only when this operation is actually invoked
/// (i.e. never pre-seeded, unlike the other profile scenarios).
///
/// This handler specifically targets submission form data by hard-coding the
/// Key to "SUBMISSIONFORM". The submission id is threaded through as additional
/// data on <see cref="ProfilePopulationMetadata"/> so plugins can build a
/// per-submission cache segment and, in Unity's case, an extra query parameter.
///
/// SubmissionId is caller-supplied (it comes from the route), so before ever
/// fetching form data — which can contain applicant PII/financial figures — the
/// handler first confirms the requested SubmissionId actually belongs to the
/// authenticated caller's own SUBMISSIONINFO list. This prevents an IDOR where
/// an authenticated user could request any other profile's submission form by
/// guessing/enumerating SubmissionId values.
/// </summary>
public class RetrieveSubmissionFormQueryHandler(
    IProfileDataRetrievalService profileDataRetrievalService,
    ILogger<RetrieveSubmissionFormQueryHandler> logger)
    : IQueryHandler<RetrieveSubmissionFormQuery, Result<ProfileData>>
{
  private const string SubmissionFormKey = "SUBMISSIONFORM";
  private const string SubmissionsKey = "SUBMISSIONINFO";

  public async Task<Result<ProfileData>> Handle(RetrieveSubmissionFormQuery request, CancellationToken cancellationToken)
  {
    logger.LogInformation("Handling retrieve submission form request for ProfileId: {ProfileId}, PluginId: {PluginId}, Provider: {Provider}, SubmissionId: {SubmissionId}",
        request.ProfileId, request.PluginId, request.Provider, request.SubmissionId);

    // Ownership check: fetch the caller's own submissions list (same path used by
    // RetrieveSubmissionsQueryHandler) and confirm the requested SubmissionId is in it
    // before ever touching SUBMISSIONFORM.
    var ownershipCheck = await profileDataRetrievalService.RetrieveProfileDataAsync(
      request.ProfileId,
      request.PluginId,
      request.Provider,
      SubmissionsKey,
      request.Subject,
      cancellationToken: cancellationToken);

    if (!ownershipCheck.IsSuccess)
    {
      logger.LogWarning(
          "Unable to verify submission ownership for ProfileId: {ProfileId}, SubmissionId: {SubmissionId}, PluginId: {PluginId}, Provider: {Provider}. Submissions list retrieval status: {Status}",
          request.ProfileId, request.SubmissionId, request.PluginId, request.Provider, ownershipCheck.Status);
      return ownershipCheck;
    }

    if (!SubmissionExistsInList(ownershipCheck.Value.Data, request.SubmissionId))
    {
      logger.LogWarning(
          "Ownership check failed: SubmissionId {SubmissionId} was not found in the submissions list for ProfileId: {ProfileId}, PluginId: {PluginId}, Provider: {Provider}",
          request.SubmissionId, request.ProfileId, request.PluginId, request.Provider);
      return Result<ProfileData>.Forbidden();
    }

    var additionalData = new Dictionary<string, object>
    {
      ["SubmissionId"] = request.SubmissionId.ToString()
    };

    return await profileDataRetrievalService.RetrieveProfileDataAsync(
      request.ProfileId,
      request.PluginId,
      request.Provider,
      SubmissionFormKey,
      request.Subject,
      additionalData,
      cancellationToken);
  }

  /// <summary>
  /// Checks whether <paramref name="submissionId"/> is present in the "submissions" array of
  /// the SUBMISSIONINFO payload (see <c>SubmissionsResponse</c>/<c>SubmissionResponse</c> for the
  /// documented shape). The Data payload is untyped and may arrive as a JsonElement, a raw JSON
  /// string, or a plain object depending on which plugin populated/cached it.
  /// </summary>
  private static bool SubmissionExistsInList(object data, Guid submissionId)
  {
    try
    {
      var json = ToJsonElement(data);
      if (!json.TryGetProperty("submissions", out var submissions) || submissions.ValueKind != JsonValueKind.Array)
      {
        return false;
      }

      var submissionIdString = submissionId.ToString();
      foreach (var submission in submissions.EnumerateArray())
      {
        if (!submission.TryGetProperty("id", out var idProperty))
        {
          continue;
        }

        var id = idProperty.ValueKind == JsonValueKind.String ? idProperty.GetString() : idProperty.ToString();
        if (string.Equals(id, submissionIdString, StringComparison.OrdinalIgnoreCase))
        {
          return true;
        }
      }

      return false;
    }
    catch (JsonException)
    {
      // Malformed/unexpected payload shape — fail closed (treat as not owned).
      return false;
    }
  }

  /// <summary>
  /// Converts the ProfileData.Data object (which may be a JsonElement, a JsonElement wrapping a
  /// JSON string, a raw string, or another plain object) to a JsonElement for parsing.
  /// </summary>
  private static JsonElement ToJsonElement(object data)
  {
    if (data is JsonElement element)
    {
      if (element.ValueKind == JsonValueKind.String)
      {
        var inner = element.GetString();
        if (!string.IsNullOrWhiteSpace(inner))
        {
          var trimmed = inner.TrimStart();
          if (trimmed.StartsWith('{') || trimmed.StartsWith('['))
          {
            return JsonSerializer.Deserialize<JsonElement>(inner);
          }
        }

        return JsonSerializer.Deserialize<JsonElement>("{}");
      }

      return element;
    }

    if (data is string str)
    {
      return string.IsNullOrWhiteSpace(str)
        ? JsonSerializer.Deserialize<JsonElement>("{}")
        : JsonSerializer.Deserialize<JsonElement>(str);
    }

    return JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(data));
  }
}
