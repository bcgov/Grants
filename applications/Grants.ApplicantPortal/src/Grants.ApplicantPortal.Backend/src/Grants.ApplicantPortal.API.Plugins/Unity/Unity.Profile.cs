using System.Text.Json;
using Grants.ApplicantPortal.API.Core;
using Grants.ApplicantPortal.API.Core.Plugins;
using Grants.ApplicantPortal.API.Infrastructure.Messaging.Messages;

namespace Grants.ApplicantPortal.API.Plugins.Unity;

/// <summary>
/// Profile population implementation for Unity plugin.
/// All profile data calls go through the single Unity endpoint
/// <c>/api/app/applicant-profiles/profile</c> with query parameters
/// <c>TenantId</c>, <c>Key</c>, <c>ProfileId</c>, and <c>Subject</c>.
/// </summary>
public partial class UnityPlugin
{
    public async Task<ProfileData> PopulateProfileAsync(ProfilePopulationMetadata metadata, CancellationToken cancellationToken = default)
    {
        var cacheSegment = $"{metadata.Provider}:{metadata.Key}";

        return await pluginCacheService.GetOrFetchAsync<ProfileData>(
            metadata.ProfileId,
            PluginId,
            cacheSegment,
            async ct =>
            {
                logger.LogInformation("Fetching {Key} from Unity API for ProfileId: {ProfileId}, Provider: {Provider}",
                    metadata.Key, metadata.ProfileId, metadata.Provider);

                var response = await CallUnityProfileAsync(metadata, ct);

                if (!response.IsSuccess)
                {
                    logger.LogError("Unity service call failed for ProfileId: {ProfileId}. Error: {Error}. StatusCode: {StatusCode}",
                        metadata.ProfileId, response.ErrorMessage, response.StatusCode);

                    throw new InvalidOperationException(
                        $"Unity service call failed for ProfileId {metadata.ProfileId}: {response.ErrorMessage} (Status: {response.StatusCode})");
                }

                // Parse the Unity API response and extract the data element,
                // stripping the internal dataType field before forwarding to the frontend.
                // Uses Utf8JsonWriter to avoid JsonObject dictionary issues with duplicate keys.
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

                await FireProfileUpdatedMessage(metadata, ct);

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
    /// Calls the Unity profile endpoint with the correct query parameters.
    /// Maps internal keys (CONTACTS, ADDRESSES, etc.) to Unity keys (CONTACTINFO, ADDRESSINFO, etc.).
    /// </summary>
    private async Task<ExternalServiceResponse<string>> CallUnityProfileAsync(
        ProfilePopulationMetadata metadata,
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
                ["Subject"] = metadata.Subject
            }
        };

        return await externalServiceClient.CallAsync(PluginId, request, cancellationToken);
    }

    /// <summary>
    /// Maps internal data keys to Unity API key names.
    /// </summary>
    private static string MapToUnityKey(string key) => key?.ToUpperInvariant() switch
    {
          "CONTACTINFO" => "CONTACTINFO",
          "ADDRESSINFO" => "ADDRESSINFO",
          "ORGINFO" => "ORGINFO",
          "SUBMISSIONINFO" => "SUBMISSIONINFO",
          "PAYMENTINFO" => "PAYMENTINFO",
        _ => key?.ToUpperInvariant() ?? string.Empty
    };

    /// <summary>
    /// Helper method to fire profile updated message
    /// </summary>
    private async Task FireProfileUpdatedMessage(ProfilePopulationMetadata metadata, CancellationToken cancellationToken)
    {
        if (messagePublisher == null)
        {
            logger.LogDebug("Message publisher not available - skipping ProfileUpdatedMessage");
            return;
        }

        try
        {
            var message = new ProfileUpdatedMessage(
                metadata.ProfileId, 
                PluginId, 
                metadata.Provider, 
                metadata.Key,
                correlationId: $"profile-{metadata.ProfileId}");

            await messagePublisher.PublishAsync(message, cancellationToken);
            
            logger.LogDebug("Published ProfileUpdatedMessage for {ProfileId}, Provider: {Provider}, Key: {Key}", 
                metadata.ProfileId, metadata.Provider, metadata.Key);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to publish ProfileUpdatedMessage for {ProfileId}", metadata.ProfileId);
            // Don't throw - messaging failures shouldn't break the main operation
        }
    }
}
