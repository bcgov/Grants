using System.Text.Json;
using Grants.ApplicantPortal.API.Core.Plugins;

namespace Grants.ApplicantPortal.API.UseCases.Addresses;

/// <summary>
/// Reads the primary address ID from the optimistically-updated cache.
/// Used by address mutation endpoints (Edit, SetAsPrimary) to return
/// the current primary without a round-trip to the external API.
/// </summary>
public static class PrimaryAddressResolver
{
  public static async Task<Guid?> GetPrimaryAddressIdAsync(
      IPluginCacheService cacheService,
      Guid profileId,
      string pluginId,
      string provider,
      CancellationToken cancellationToken)
  {
    try
    {
      var cacheSegment = $"{provider}:ADDRESSINFO";
      var cached = await cacheService.TryGetAsync<ProfileData>(
          profileId, pluginId, cacheSegment, cancellationToken);

      if (cached == null)
        return null;

      // Data can be a JsonElement (Unity) or a JSON string (Demo).
      // Normalize to a parsed JsonElement before searching.
      var root = ResolveDataElement(cached.Data);
      if (root == null)
        return null;

      if (!root.Value.TryGetProperty("addresses", out var addresses) ||
          addresses.ValueKind != JsonValueKind.Array)
        return null;

      foreach (var address in addresses.EnumerateArray())
      {
        if (address.TryGetProperty("isPrimary", out var isPrimary) && isPrimary.GetBoolean() &&
            address.TryGetProperty("id", out var idProp))
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
