using System.Globalization;
using System.Text.Json;
using Grants.ApplicantPortal.API.Core.Plugins;

namespace Grants.ApplicantPortal.API.UseCases.Addresses;

/// <summary>
/// Reads the primary address ID of every address type from the optimistically-updated cache.
/// Used by address mutation endpoints (Create, Edit, SetAsPrimary, Delete) to return
/// the current primaries without a round-trip to the external API.
/// An applicant may hold one primary address per address type (e.g. Physical, Mailing,
/// Business), so the result is keyed by address type rather than being a single ID.
/// </summary>
public static class PrimaryAddressResolver
{
  /// <summary>
  /// Resolves the primary address ID for each address type present in the cache.
  /// Within a type group the address explicitly flagged <c>isPrimary</c> wins; when no
  /// address of that type is flagged, the most recently created one is inferred as primary.
  /// Returns an empty dictionary when nothing can be resolved. Keys are compared case-insensitively.
  /// </summary>
  public static async Task<IReadOnlyDictionary<string, Guid>> GetPrimaryAddressIdsByTypeAsync(
      IPluginCacheService cacheService,
      Guid profileId,
      string pluginId,
      string provider,
      CancellationToken cancellationToken)
  {
    var primaryIdsByType = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);

    try
    {
      var cacheSegment = $"{provider}:ADDRESSINFO";
      var cached = await cacheService.TryGetAsync<ProfileData>(
          profileId, pluginId, cacheSegment, cancellationToken);

      if (cached == null)
        return primaryIdsByType;

      // Data can be a JsonElement (Unity) or a JSON string (Demo).
      // Normalize to a parsed JsonElement before searching.
      var root = ResolveDataElement(cached.Data);
      if (root == null)
        return primaryIdsByType;

      if (!root.Value.TryGetProperty("addresses", out var addresses) ||
          addresses.ValueKind != JsonValueKind.Array)
        return primaryIdsByType;

      // Group the addresses by their type. Document order is preserved inside each group.
      var groups = new Dictionary<string, List<AddressEntry>>(StringComparer.OrdinalIgnoreCase);

      foreach (var address in addresses.EnumerateArray())
      {
        if (address.ValueKind != JsonValueKind.Object ||
            !address.TryGetProperty("id", out var idProp) ||
            idProp.ValueKind != JsonValueKind.String ||
            !Guid.TryParse(idProp.GetString(), out var addressId))
          continue;

        var typeKey = ReadAddressTypeKey(address);

        if (!groups.TryGetValue(typeKey, out var entries))
        {
          entries = new List<AddressEntry>();
          groups[typeKey] = entries;
        }

        entries.Add(new AddressEntry(addressId, ReadIsPrimary(address), ReadCreationTime(address)));
      }

      // Resolve one primary per type: the flagged address wins, otherwise infer the latest one.
      foreach (var group in groups)
      {
        var resolved = ResolveGroupPrimary(group.Value);
        if (resolved.HasValue)
          primaryIdsByType[group.Key] = resolved.Value;
      }

      return primaryIdsByType;
    }
    catch
    {
      return primaryIdsByType;
    }
  }

  /// <summary>
  /// Picks the primary address of a single address-type group.
  /// Prefers the first address flagged as primary (duplicate flags resolve deterministically),
  /// otherwise falls back to the address with the latest creationTime, otherwise the first one.
  /// </summary>
  private static Guid? ResolveGroupPrimary(List<AddressEntry> entries)
  {
    if (entries.Count == 0)
      return null;

    foreach (var entry in entries)
    {
      if (entry.IsPrimary)
        return entry.Id;
    }

    var best = entries[0];
    foreach (var entry in entries)
    {
      if (entry.CreationTime.HasValue &&
          (!best.CreationTime.HasValue || entry.CreationTime.Value > best.CreationTime.Value))
      {
        best = entry;
      }
    }

    return best.Id;
  }

  /// <summary>
  /// Reads the address type used as the grouping key. A missing, non-string or blank
  /// value is mapped to a stable placeholder key so resolution never fails.
  /// </summary>
  private static string ReadAddressTypeKey(JsonElement address)
  {
    if (address.TryGetProperty("addressType", out var typeProp) &&
        typeProp.ValueKind == JsonValueKind.String)
    {
      return AddressTypeKey.Normalize(typeProp.GetString());
    }

    return AddressTypeKey.Unknown;
  }

  private static bool ReadIsPrimary(JsonElement address)
    => address.TryGetProperty("isPrimary", out var isPrimary) && isPrimary.ValueKind == JsonValueKind.True;

  private static DateTimeOffset? ReadCreationTime(JsonElement address)
  {
    if (address.TryGetProperty("creationTime", out var creationTime) &&
        creationTime.ValueKind == JsonValueKind.String &&
        DateTimeOffset.TryParse(
            creationTime.GetString(),
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
            out var parsed))
    {
      return parsed;
    }

    return null;
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

  /// <summary>
  /// Minimal projection of a cached address used while resolving primaries.
  /// </summary>
  private readonly record struct AddressEntry(Guid Id, bool IsPrimary, DateTimeOffset? CreationTime);
}
