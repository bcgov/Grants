using System.Text.Json;
using Grants.ApplicantPortal.API.Core;
using Grants.ApplicantPortal.API.Core.Plugins;
using Grants.ApplicantPortal.API.Infrastructure.Messaging.Messages;

namespace Grants.ApplicantPortal.API.Plugins.Unity;

/// <summary>
/// Profile population implementation for Unity plugin
/// </summary>
public partial class UnityPlugin
{
    public async Task<ProfileData> PopulateProfileAsync(ProfilePopulationMetadata metadata, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Unity plugin populating profile for ProfileId: {ProfileId}", metadata.ProfileId);

        try
        {
            // Use the external service client to get data from Unity APIs
            var response = await CallUnityServiceAsync(metadata, cancellationToken);

            if (!response.IsSuccess)
            {
                _logger.LogError("Unity service call failed for ProfileId: {ProfileId}. Error: {Error}. StatusCode: {StatusCode}", 
                    metadata.ProfileId, response.ErrorMessage, response.StatusCode);
                
                throw new InvalidOperationException(
                    $"Unity service call failed for ProfileId {metadata.ProfileId}: {response.ErrorMessage} (Status: {response.StatusCode})");
            }

            _logger.LogInformation("Unity plugin successfully populated profile for ProfileId: {ProfileId}", metadata.ProfileId);

            // Parse the Unity Mock API response to extract the data portion
            var mockApiResponse = JsonSerializer.Deserialize<JsonElement>(response.Data!);
            var dataElement = mockApiResponse.GetProperty("data");
            
            // The data is already a JSON string from the mock API, so just extract it as a string
            var dataJson = dataElement.GetString()!;

            // 🔥 Fire a message when profile data is populated
            await FireProfileUpdatedMessage(metadata, cancellationToken);

            return new ProfileData(
                metadata.ProfileId,
                metadata.PluginId,
                metadata.Provider,
                metadata.Key,
                dataJson);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unity plugin failed to populate profile for ProfileId: {ProfileId}", metadata.ProfileId);
            throw;
        }
    }

    private async Task<ExternalServiceResponse<string>> CallUnityServiceAsync(
        ProfilePopulationMetadata metadata, 
        CancellationToken cancellationToken)
    {
        var endpoint = BuildEndpoint(metadata.Provider, metadata.Key, metadata.ProfileId);
        
        var request = new ExternalServiceRequest
        {
            Endpoint = endpoint,
            Method = HttpMethod.Get,
            QueryParameters = new Dictionary<string, string>
            {
                ["profileId"] = metadata.ProfileId.ToString(),
                ["provider"] = metadata.Provider,
                ["key"] = metadata.Key
            }
        };

        // Add any additional data as query parameters
        if (metadata.AdditionalData?.Any() == true)
        {
            foreach (var kvp in metadata.AdditionalData)
            {
                request.QueryParameters[kvp.Key] = kvp.Value?.ToString() ?? string.Empty;
            }
        }

        return await _externalServiceClient.CallAsync(PluginId, request, cancellationToken);
    }

    private static string BuildEndpoint(string provider, string key, Guid profileId)
    {
        return (key?.ToUpper()) switch
        {
            "PROFILE" => $"/api/v1/profiles/{profileId}",
            "EMPLOYMENT" => $"/api/v1/profiles/{profileId}/employment",
            "SECURITY" => $"/api/v1/profiles/{profileId}/security",
            "CONTACTS" => $"/api/v1/profiles/{profileId}/contacts",
            "ADDRESSES" => $"/api/v1/profiles/{profileId}/addresses",
            "ORGINFO" => $"/api/v1/profiles/{profileId}/organization",
            "SUBMISSIONS" => $"/api/v1/profiles/{profileId}/submissions",
            "PAYMENTS" => $"/api/v1/profiles/{profileId}/payments",
            _ => $"/api/v1/profiles/{profileId}/data"
        };
    }

    /// <summary>
    /// Helper method to fire profile updated message
    /// </summary>
    private async Task FireProfileUpdatedMessage(ProfilePopulationMetadata metadata, CancellationToken cancellationToken)
    {
        if (_messagePublisher == null)
        {
            _logger.LogDebug("Message publisher not available - skipping ProfileUpdatedMessage");
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

            await _messagePublisher.PublishAsync(message, cancellationToken);
            
            _logger.LogDebug("Published ProfileUpdatedMessage for {ProfileId}, Provider: {Provider}, Key: {Key}", 
                metadata.ProfileId, metadata.Provider, metadata.Key);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to publish ProfileUpdatedMessage for {ProfileId}", metadata.ProfileId);
            // Don't throw - messaging failures shouldn't break the main operation
        }
    }
}
