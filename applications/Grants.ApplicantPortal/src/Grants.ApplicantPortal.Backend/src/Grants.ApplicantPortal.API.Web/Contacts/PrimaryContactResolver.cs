using System.Text.Json;
using Grants.ApplicantPortal.API.Core.Plugins;
using Grants.ApplicantPortal.API.UseCases;

namespace Grants.ApplicantPortal.API.Web.Contacts;

/// <summary>
/// Reads the primary contact ID from the optimistically-updated cache.
/// Used by contact mutation endpoints (Create, Edit, Delete) to return
/// the current primary without a round-trip to the external API.
/// </summary>
public static class PrimaryContactResolver
{
  public static async Task<Guid?> GetPrimaryContactIdAsync(
      IPluginCacheService cacheService,
      Guid profileId,
      string pluginId,
      string provider,
      CancellationToken cancellationToken)
  {
    try
    {
      var cacheSegment = $"{provider}:CONTACTINFO";
      var cached = await cacheService.TryGetAsync<ProfileData>(
          profileId, pluginId, cacheSegment, cancellationToken);

      if (cached == null)
        return null;

      // Data can be a JsonElement (Unity) or a JSON string (Demo).
      // Normalize to a parsed JsonElement before searching.
      var root = ResolveDataElement(cached.Data);
      if (root == null)
        return null;

      if (!root.Value.TryGetProperty("contacts", out var contacts) ||
          contacts.ValueKind != JsonValueKind.Array)
        return null;

      foreach (var contact in contacts.EnumerateArray())
      {
        if (contact.TryGetProperty("isPrimary", out var isPrimary) && isPrimary.GetBoolean() &&
            contact.TryGetProperty("contactId", out var idProp))
        {
          if (Guid.TryParse(idProp.GetString(), out var primaryId))
            return primaryId;
        }
      }

      return null;
    }
    catch
    {
      return null;
    }
  }

  /// <summary>
  /// Normalizes the ProfileData.Data value (object) into a JsonElement.
  /// Handles both JsonElement (from Unity) and string (from Demo) storage shapes.
  /// </summary>
  private static JsonElement? ResolveDataElement(object data)
  {
    if (data is JsonElement element)
    {
      // If the JsonElement is a string (double-serialized), unwrap it
      if (element.ValueKind == JsonValueKind.String)
      {
        var inner = element.GetString();
        if (inner != null)
          return JsonDocument.Parse(inner).RootElement;
      }
      return element;
    }

    // Data stored as a raw string (e.g. Demo plugin serialized to JSON string)
    if (data is string jsonString)
      return JsonDocument.Parse(jsonString).RootElement;

    // Fallback: serialize then parse
    var json = JsonSerializer.Serialize(data);
    return JsonDocument.Parse(json).RootElement;
  }
}
