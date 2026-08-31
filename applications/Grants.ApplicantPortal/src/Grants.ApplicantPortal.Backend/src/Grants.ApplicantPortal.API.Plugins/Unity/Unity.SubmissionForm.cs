using System.Text.Json;
using Grants.ApplicantPortal.API.Core;
using Grants.ApplicantPortal.API.Core.Plugins;

namespace Grants.ApplicantPortal.API.Plugins.Unity;

/// <summary>
/// Submission form retrieval for the Unity plugin. Calls the Unity profile endpoint
/// with an additional <c>SubmissionId</c> query parameter and caches the result per
/// submission. There is no live Unity endpoint for this in dev/test environments —
/// a failed/unreachable call is surfaced as an exception from the cache factory,
/// which <see cref="Grants.ApplicantPortal.API.UseCases.ProfileDataRetrievalService"/>
/// catches and converts into an Ardalis <c>Result.Error</c>, never an unhandled exception.
/// </summary>
public partial class UnityPlugin
{
    internal const string SubmissionFormKey = "SUBMISSIONFORM";

    private async Task<ProfileData> PopulateSubmissionFormAsync(ProfilePopulationMetadata metadata, CancellationToken cancellationToken)
    {
        var submissionId = GetSubmissionId(metadata);
        var cacheSegment = $"{metadata.Provider}:SUBMISSIONFORM:{submissionId}";

        return await pluginCacheService.GetOrFetchAsync<ProfileData>(
            metadata.ProfileId,
            PluginId,
            cacheSegment,
            async ct =>
            {
                logger.LogInformation("Fetching submission form from Unity API for ProfileId: {ProfileId}, Provider: {Provider}, SubmissionId: {SubmissionId}",
                    metadata.ProfileId, metadata.Provider, submissionId);

                var response = await CallUnitySubmissionFormAsync(metadata, submissionId, ct);

                if (!response.IsSuccess)
                {
                    logger.LogError("Unity submission form call failed for ProfileId: {ProfileId}, SubmissionId: {SubmissionId}. Error: {Error}. StatusCode: {StatusCode}",
                        metadata.ProfileId, submissionId, response.ErrorMessage, response.StatusCode);

                    throw new InvalidOperationException(
                        $"Unity submission form call failed for ProfileId {metadata.ProfileId}, SubmissionId {submissionId}: {response.ErrorMessage} (Status: {response.StatusCode})");
                }

                // Parse the Unity API response and extract the data element,
                // stripping the internal dataType field before forwarding to the frontend
                // (mirrors the approach used for other profile keys in Unity.Profile.cs).
                var apiResponse = JsonSerializer.Deserialize<JsonElement>(response.Data!);
                var dataElement = apiResponse.GetProperty("data");

                using var stream = new MemoryStream();
                using (var writer = new Utf8JsonWriter(stream))
                {
                    writer.WriteStartObject();
                    foreach (var property in dataElement.EnumerateObject())
                    {
                        if (!property.NameEquals("dataType"))
                        {
                            property.WriteTo(writer);
                        }
                    }
                    writer.WriteEndObject();
                }

                var cleanedData = JsonSerializer.Deserialize<JsonElement>(stream.ToArray());

                return new ProfileData(
                    metadata.ProfileId,
                    metadata.PluginId,
                    metadata.Provider,
                    metadata.Key,
                    cleanedData);
            },
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Calls the Unity profile endpoint for a single submission's form, adding the
    /// submission id as an extra query parameter alongside the standard profile params.
    /// </summary>
    private async Task<ExternalServiceResponse<string>> CallUnitySubmissionFormAsync(
        ProfilePopulationMetadata metadata,
        string submissionId,
        CancellationToken cancellationToken)
    {
        var unityKey = MapToUnityKey(metadata.Key);

        var request = new ExternalServiceRequest
        {
            Endpoint = "/api/app/applicant-profiles/profile",
            Method = HttpMethod.Get,
            QueryParameters = new Dictionary<string, string>
            {
                ["TenantId"] = metadata.Provider,
                ["Key"] = unityKey,
                ["ProfileId"] = metadata.ProfileId.ToString(),
                ["Subject"] = metadata.Subject,
                ["SubmissionId"] = submissionId
            }
        };

        return await externalServiceClient.CallAsync(PluginId, request, cancellationToken);
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
