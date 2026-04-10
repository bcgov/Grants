using System.Text.Json;
using Grants.ApplicantPortal.API.Core.Plugins;

namespace Grants.ApplicantPortal.API.UseCases.Contacts;

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

      // First pass: look for a contact explicitly marked as primary
      foreach (var contact in contacts.EnumerateArray())
      {
        if (contact.TryGetProperty("isPrimary", out var isPrimary) && isPrimary.GetBoolean() &&
            contact.TryGetProperty("contactId", out var idProp))
        {
          if (Guid.TryParse(idProp.GetString(), out var primaryId))
            return primaryId;
        }
      }

      // Fallback: no contact is marked as primary — pick the one with the latest creationTime
      Guid? fallbackId = null;
      DateTimeOffset latestTime = DateTimeOffset.MinValue;

      foreach (var contact in contacts.EnumerateArray())
      {
        if (!contact.TryGetProperty("contactId", out var idProp) ||
            !Guid.TryParse(idProp.GetString(), out var contactId))
          continue;

        if (contact.TryGetProperty("creationTime", out var ctProp) &&
            DateTimeOffset.TryParse(ctProp.GetString(), out var ct) &&
            ct > latestTime)
        {
          latestTime = ct;
          fallbackId = contactId;
        }
        else if (fallbackId == null)
        {
          // If no creationTime, use the first contact as ultimate fallback
          fallbackId = contactId;
        }
      }

      return fallbackId;
    }
    catch
    {
      return null;
    }
  }

  /// <summary>
  /// Normalizes the ProfileData.Data value (object) into a JsonElement.
  /// Handles both JsonElement (from Unity) and string (from Demo) storage shapes.
  /// Each JsonDocument is disposed after cloning the root element to avoid memory leaks.
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
        {
          using var doc = JsonDocument.Parse(inner);
          return doc.RootElement.Clone();
        }
      }
      return element;
    }

    // Data stored as a raw string (e.g. Demo plugin serialized to JSON string)
    if (data is string jsonString)
    {
      using var doc = JsonDocument.Parse(jsonString);
      return doc.RootElement.Clone();
    }

    // Fallback: serialize then parse
    var json = JsonSerializer.Serialize(data);
    using var fallbackDoc = JsonDocument.Parse(json);
    return fallbackDoc.RootElement.Clone();
  }
}
