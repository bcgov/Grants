using System.Text.Json;
using Grants.ApplicantPortal.API.Core.Plugins;

namespace Grants.ApplicantPortal.API.Plugins.Unity;

/// <summary>
/// Shared optimistic cache-update helpers for all Unity write operations.
/// Each helper reads the current cache, patches the relevant JSON array or object,
/// and writes it back so the UI sees changes immediately while the command is still
/// in the outbox pipeline.
/// </summary>
public partial class UnityPlugin
{
  private static readonly JsonSerializerOptions _camelCase = new()
  {
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
  };

  /// <summary>
  /// Reads the cached ProfileData for a given data key (e.g. CONTACTINFO, ADDRESSINFO, ORGINFO),
  /// applies a transform to its Data JsonElement, and writes it back.
  /// Returns false if no cache entry exists (caller should not treat this as an error).
  /// </summary>
  private async Task<bool> PatchCachedProfileDataAsync(
      Guid profileId,
      string provider,
      string dataKey,
      Func<JsonElement, JsonElement?> transform,
      CancellationToken cancellationToken)
  {
    try
    {
      var cacheSegment = $"{provider}:{dataKey}";
      var cached = await pluginCacheService.TryGetAsync<ProfileData>(
          profileId, PluginId, cacheSegment, cancellationToken);

      if (cached == null)
      {
        logger.LogDebug("No cached {DataKey} to patch for ProfileId: {ProfileId} — next read will fetch from API",
            dataKey, profileId);
        return false;
      }

      var dataJson = JsonSerializer.Serialize(cached.Data);
      using var doc = JsonDocument.Parse(dataJson);

      var patched = transform(doc.RootElement);
      if (patched == null)
      {
        logger.LogDebug("Transform returned null for {DataKey}, ProfileId: {ProfileId} — skipping cache write",
            dataKey, profileId);
        return false;
      }

      var updatedProfileData = new ProfileData(
          cached.ProfileId, cached.PluginId, cached.Provider, cached.Key, patched.Value, cached.PopulatedAt)
      {
        CacheStatus = cached.CacheStatus,
        CacheStore = cached.CacheStore
      };

      await pluginCacheService.SetAsync(profileId, PluginId, cacheSegment, updatedProfileData, cancellationToken);
      return true;
    }
    catch (Exception ex)
    {
      logger.LogWarning(ex, "Failed to patch cached {DataKey} for ProfileId: {ProfileId}", dataKey, profileId);
      return false;
    }
  }

  /// <summary>
  /// Rebuilds a JSON object, replacing a named array property with a new array built by the callback.
  /// All other properties are preserved as-is.
  /// </summary>
  private static JsonElement RebuildWithArray(
      JsonElement root,
      string arrayPropertyName,
      Action<Utf8JsonWriter, JsonElement> writeArrayContents)
  {
    using var stream = new MemoryStream();
    using (var writer = new Utf8JsonWriter(stream))
    {
      writer.WriteStartObject();
      foreach (var prop in root.EnumerateObject())
      {
        if (prop.NameEquals(arrayPropertyName) && prop.Value.ValueKind == JsonValueKind.Array)
        {
          writer.WritePropertyName(arrayPropertyName);
          writer.WriteStartArray();
          writeArrayContents(writer, prop.Value);
          writer.WriteEndArray();
        }
        else
        {
          prop.WriteTo(writer);
        }
      }
      writer.WriteEndObject();
    }
    return JsonSerializer.Deserialize<JsonElement>(stream.ToArray());
  }

  /// <summary>
  /// Rebuilds a JSON object, replacing a named object property with a patched version.
  /// </summary>
  private static JsonElement RebuildWithObject(
      JsonElement root,
      string objectPropertyName,
      Func<JsonElement, object> patchObject)
  {
    using var stream = new MemoryStream();
    using (var writer = new Utf8JsonWriter(stream))
    {
      writer.WriteStartObject();
      foreach (var prop in root.EnumerateObject())
      {
        if (prop.NameEquals(objectPropertyName) && prop.Value.ValueKind == JsonValueKind.Object)
        {
          writer.WritePropertyName(objectPropertyName);
          var patched = patchObject(prop.Value);
          JsonSerializer.Serialize(writer, patched, _camelCase);
        }
        else
        {
          prop.WriteTo(writer);
        }
      }
      writer.WriteEndObject();
    }
    return JsonSerializer.Deserialize<JsonElement>(stream.ToArray());
  }
}
