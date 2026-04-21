using System.Text.Json;
using Grants.ApplicantPortal.API.Core.DTOs;
using Grants.ApplicantPortal.API.Core.Plugins;
using Grants.ApplicantPortal.API.Core.Services;
using Microsoft.Extensions.Logging;

namespace Grants.ApplicantPortal.API.UseCases.Security;

/// <summary>
/// Validates resource ownership by cross-referencing client-supplied IDs against
/// the authenticated user's cached profile data. Prevents IDOR attacks.
///
/// Cache keys follow the convention: {prefix}{profileId}:{pluginId}:{provider}:{dataKey}
/// Data is always scoped to the authenticated user's profileId (resolved from JWT, never client-supplied).
/// </summary>
public class ResourceOwnershipValidator(
  IPluginCacheService pluginCacheService,
  IProfilePluginFactory pluginFactory,
  ILogger<ResourceOwnershipValidator> logger) : IResourceOwnershipValidator
{
  // Cache segment keys used by both Unity and Demo plugins
  private const string ContactInfoSegment = "CONTACTINFO";
  private const string AddressInfoSegment = "ADDRESSINFO";
  private const string OrgInfoSegment = "ORGINFO";

  public async Task<OwnershipValidationResult> ValidateContactOwnershipAsync(
    Guid contactId,
    ProfileContext profileContext,
    CancellationToken cancellationToken = default)
  {
    var cacheSegment = $"{profileContext.Provider}:{ContactInfoSegment}";

    var cached = await pluginCacheService.TryGetAsync<ProfileData>(
      profileContext.ProfileId, profileContext.PluginId, cacheSegment, cancellationToken);

    if (cached == null)
    {
      // Attempt to hydrate from plugin
      cached = await HydrateProfileDataAsync(profileContext, ContactInfoSegment, cancellationToken);
    }

    if (cached == null)
    {
      logger.LogWarning(
        "Resource ownership check failed: no contact data available for ProfileId: {ProfileId}, Plugin: {PluginId}, Provider: {Provider}",
        profileContext.ProfileId, profileContext.PluginId, profileContext.Provider);
      return OwnershipValidationResult.NotOwned("Unable to verify contact ownership — profile data not available");
    }

    return FindContactInCachedData(cached.Data, contactId, profileContext);
  }

  public async Task<OwnershipValidationResult> ValidateAddressOwnershipAsync(
    Guid addressId,
    ProfileContext profileContext,
    CancellationToken cancellationToken = default)
  {
    var cacheSegment = $"{profileContext.Provider}:{AddressInfoSegment}";

    var cached = await pluginCacheService.TryGetAsync<ProfileData>(
      profileContext.ProfileId, profileContext.PluginId, cacheSegment, cancellationToken);

    if (cached == null)
    {
      cached = await HydrateProfileDataAsync(profileContext, AddressInfoSegment, cancellationToken);
    }

    if (cached == null)
    {
      logger.LogWarning(
        "Resource ownership check failed: no address data available for ProfileId: {ProfileId}, Plugin: {PluginId}, Provider: {Provider}",
        profileContext.ProfileId, profileContext.PluginId, profileContext.Provider);
      return OwnershipValidationResult.NotOwned("Unable to verify address ownership — profile data not available");
    }

    return FindAddressInCachedData(cached.Data, addressId, profileContext);
  }

  public async Task<OwnershipValidationResult> ValidateOrganizationOwnershipAsync(
    Guid organizationId,
    ProfileContext profileContext,
    CancellationToken cancellationToken = default)
  {
    var cacheSegment = $"{profileContext.Provider}:{OrgInfoSegment}";

    var cached = await pluginCacheService.TryGetAsync<ProfileData>(
      profileContext.ProfileId, profileContext.PluginId, cacheSegment, cancellationToken);

    if (cached == null)
    {
      cached = await HydrateProfileDataAsync(profileContext, OrgInfoSegment, cancellationToken);
    }

    if (cached == null)
    {
      logger.LogWarning(
        "Resource ownership check failed: no organization data available for ProfileId: {ProfileId}, Plugin: {PluginId}, Provider: {Provider}",
        profileContext.ProfileId, profileContext.PluginId, profileContext.Provider);
      return OwnershipValidationResult.NotOwned("Unable to verify organization ownership — profile data not available");
    }

    return FindOrganizationInCachedData(cached.Data, organizationId, profileContext);
  }

  public async Task<OwnershipValidationResult> ValidateApplicantOwnershipAsync(
    Guid applicantId,
    ProfileContext profileContext,
    CancellationToken cancellationToken = default)
  {
    if (applicantId == Guid.Empty)
    {
      return OwnershipValidationResult.NotOwned("ApplicantId is required");
    }

    // The applicantId corresponds to an organization ID in the ORGINFO cache.
    // Validate that the authenticated user owns an organization with this ID.
    var cacheSegment = $"{profileContext.Provider}:{OrgInfoSegment}";
    var cached = await pluginCacheService.TryGetAsync<ProfileData>(
      profileContext.ProfileId, profileContext.PluginId, cacheSegment, cancellationToken);

    if (cached == null)
    {
      cached = await HydrateProfileDataAsync(profileContext, OrgInfoSegment, cancellationToken);
    }

    if (cached == null)
    {
      logger.LogWarning(
        "Applicant ownership check failed: no organization data available for ProfileId: {ProfileId}, Plugin: {PluginId}, Provider: {Provider}",
        profileContext.ProfileId, profileContext.PluginId, profileContext.Provider);
      return OwnershipValidationResult.NotOwned("Unable to verify applicant ownership — profile data not available");
    }

    return FindApplicantIdInCachedData(cached.Data, applicantId, profileContext);
  }

  /// <summary>
  /// Searches the cached contact data for a contact with the given ID.
  /// The data is a JSON structure with a "contacts" array containing objects
  /// with "contactId" and "isEditable" properties.
  /// </summary>
  private OwnershipValidationResult FindContactInCachedData(object data, Guid contactId, ProfileContext profileContext)
  {
    try
    {
      var json = SerializeToJsonElement(data);
      var contactIdString = contactId.ToString();

      if (json.TryGetProperty("contacts", out var contacts) && contacts.ValueKind == JsonValueKind.Array)
      {
        foreach (var contact in contacts.EnumerateArray())
        {
          if (TryGetStringProperty(contact, "contactId", out var id) &&
              string.Equals(id, contactIdString, StringComparison.OrdinalIgnoreCase))
          {
            var isEditable = !contact.TryGetProperty("isEditable", out var editableProp) || editableProp.GetBoolean();
            return OwnershipValidationResult.Success(isEditable);
          }
        }
      }

      logger.LogWarning(
        "Contact {ContactId} not found in cached data for ProfileId: {ProfileId}, Plugin: {PluginId}, Provider: {Provider}",
        contactId, profileContext.ProfileId, profileContext.PluginId, profileContext.Provider);
      return OwnershipValidationResult.NotOwned($"Contact '{contactId}' does not belong to the authenticated user");
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Error parsing cached contact data for ownership check. ProfileId: {ProfileId}", profileContext.ProfileId);
      return OwnershipValidationResult.NotOwned("Unable to verify contact ownership — data parsing error");
    }
  }

  /// <summary>
  /// Searches the cached address data for an address with the given ID.
  /// The data is a JSON structure with an "addresses" array containing objects
  /// with "id" and "isEditable" properties.
  /// </summary>
  private OwnershipValidationResult FindAddressInCachedData(object data, Guid addressId, ProfileContext profileContext)
  {
    try
    {
      var json = SerializeToJsonElement(data);
      var addressIdString = addressId.ToString();

      if (json.TryGetProperty("addresses", out var addresses) && addresses.ValueKind == JsonValueKind.Array)
      {
        foreach (var address in addresses.EnumerateArray())
        {
          if (TryGetStringProperty(address, "id", out var id) &&
              string.Equals(id, addressIdString, StringComparison.OrdinalIgnoreCase))
          {
            var isEditable = !address.TryGetProperty("isEditable", out var editableProp) || editableProp.GetBoolean();
            return OwnershipValidationResult.Success(isEditable);
          }
        }
      }

      logger.LogWarning(
        "Address {AddressId} not found in cached data for ProfileId: {ProfileId}, Plugin: {PluginId}, Provider: {Provider}",
        addressId, profileContext.ProfileId, profileContext.PluginId, profileContext.Provider);
      return OwnershipValidationResult.NotOwned($"Address '{addressId}' does not belong to the authenticated user");
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Error parsing cached address data for ownership check. ProfileId: {ProfileId}", profileContext.ProfileId);
      return OwnershipValidationResult.NotOwned("Unable to verify address ownership — data parsing error");
    }
  }

  /// <summary>
  /// Searches the cached organization data for an organization with the given ID.
  /// </summary>
  private OwnershipValidationResult FindOrganizationInCachedData(object data, Guid organizationId, ProfileContext profileContext)
  {
    try
    {
      var json = SerializeToJsonElement(data);
      var orgIdString = organizationId.ToString();

      // Check organizations array — both Unity and Demo plugins store orgs with an "id" property
      if (json.TryGetProperty("organizations", out var orgs) && orgs.ValueKind == JsonValueKind.Array)
      {
        foreach (var org in orgs.EnumerateArray())
        {
          if (TryGetStringProperty(org, "id", out var id) &&
              string.Equals(id, orgIdString, StringComparison.OrdinalIgnoreCase))
          {
            return OwnershipValidationResult.Success();
          }
        }
      }

      logger.LogWarning(
        "Organization {OrganizationId} not found in cached data for ProfileId: {ProfileId}, Plugin: {PluginId}, Provider: {Provider}",
        organizationId, profileContext.ProfileId, profileContext.PluginId, profileContext.Provider);
      return OwnershipValidationResult.NotOwned($"Organization '{organizationId}' does not belong to the authenticated user");
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Error parsing cached organization data for ownership check. ProfileId: {ProfileId}", profileContext.ProfileId);
      return OwnershipValidationResult.NotOwned("Unable to verify organization ownership — data parsing error");
    }
  }

  /// <summary>
  /// Searches the cached organization data for an organization whose ID matches the given applicantId.
  /// The applicantId corresponds to an organization ID in the ORGINFO cache.
  /// </summary>
  private OwnershipValidationResult FindApplicantIdInCachedData(object data, Guid applicantId, ProfileContext profileContext)
  {
    try
    {
      var json = SerializeToJsonElement(data);
      var applicantIdString = applicantId.ToString();

      // Check organizations array — applicantId maps to an organization "id"
      if (json.TryGetProperty("organizations", out var orgs) && orgs.ValueKind == JsonValueKind.Array)
      {
        foreach (var org in orgs.EnumerateArray())
        {
          if (TryGetStringProperty(org, "id", out var id) &&
              string.Equals(id, applicantIdString, StringComparison.OrdinalIgnoreCase))
          {
            return OwnershipValidationResult.Success();
          }
        }
      }

      logger.LogWarning(
        "ApplicantId {ApplicantId} not found in organization data for ProfileId: {ProfileId}, Plugin: {PluginId}, Provider: {Provider}",
        applicantId, profileContext.ProfileId, profileContext.PluginId, profileContext.Provider);
      return OwnershipValidationResult.NotOwned($"ApplicantId '{applicantId}' does not belong to the authenticated user");
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Error parsing cached data for applicant ownership check. ProfileId: {ProfileId}", profileContext.ProfileId);
      return OwnershipValidationResult.NotOwned("Unable to verify applicant ownership — data parsing error");
    }
  }

  /// <summary>
  /// Attempts to hydrate profile data from the plugin when the cache is empty.
  /// Returns null if hydration fails (fail-closed).
  /// </summary>
  private async Task<ProfileData?> HydrateProfileDataAsync(
    ProfileContext profileContext, string dataKey, CancellationToken cancellationToken)
  {
    try
    {
      var plugin = pluginFactory.GetPlugin(profileContext.PluginId);
      if (plugin == null) return null;

      var metadata = new ProfilePopulationMetadata(
        profileContext.ProfileId,
        profileContext.PluginId,
        profileContext.Provider,
        dataKey,
        profileContext.Subject ?? "");

      var profileData = await plugin.PopulateProfileAsync(metadata, cancellationToken);
      return profileData;
    }
    catch (Exception ex)
    {
      logger.LogWarning(ex,
        "Failed to hydrate {DataKey} for ownership validation. ProfileId: {ProfileId}, Plugin: {PluginId}",
        dataKey, profileContext.ProfileId, profileContext.PluginId);
      return null;
    }
  }

  /// <summary>
  /// Converts the cached Data object (which may be a JsonElement, a raw JSON string, or another type) to a JsonElement for parsing.
  /// </summary>
  private static JsonElement SerializeToJsonElement(object data)
  {
    if (data is JsonElement element)
    {
      // Some plugins (e.g. Demo) serialize the ProfileData.Data payload as a JSON string,
      // which round-trips through JsonElement as ValueKind.String. Parse the inner JSON in that case.
      if (element.ValueKind == JsonValueKind.String)
      {
        var inner = element.GetString();
        if (!string.IsNullOrWhiteSpace(inner))
        {
          return JsonSerializer.Deserialize<JsonElement>(inner);
        }
      }
      return element;
    }

    if (data is string str)
    {
      if (string.IsNullOrWhiteSpace(str))
      {
        return JsonSerializer.Deserialize<JsonElement>("{}");
      }
      return JsonSerializer.Deserialize<JsonElement>(str);
    }

    var json = JsonSerializer.Serialize(data);
    return JsonSerializer.Deserialize<JsonElement>(json);
  }

  /// <summary>
  /// Tries to read a string property from a JsonElement, handling both string and Guid value kinds.
  /// </summary>
  private static bool TryGetStringProperty(JsonElement element, string propertyName, out string value)
  {
    value = string.Empty;
    if (!element.TryGetProperty(propertyName, out var prop))
      return false;

    if (prop.ValueKind == JsonValueKind.String)
    {
      value = prop.GetString() ?? string.Empty;
      return !string.IsNullOrEmpty(value);
    }

    // Handle case where the property is stored as a non-string (e.g., raw guid)
    value = prop.ToString();
    return !string.IsNullOrEmpty(value);
  }
}
