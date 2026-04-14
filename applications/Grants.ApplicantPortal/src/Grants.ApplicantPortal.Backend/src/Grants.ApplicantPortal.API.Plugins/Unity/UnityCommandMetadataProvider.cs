using System.Text.Json;
using Grants.ApplicantPortal.API.Core.Plugins;

namespace Grants.ApplicantPortal.API.Plugins.Unity;

/// <summary>
/// Provides Unity-specific command metadata: cache segment mapping, friendly names,
/// and payload parsing. Registered in DI so the event system can resolve it.
/// </summary>
public class UnityCommandMetadataProvider : IPluginCommandMetadataProvider
{
  public string PluginId => "UNITY";

  private static readonly Dictionary<string, string> CacheSegments = new(StringComparer.OrdinalIgnoreCase)
  {
    ["CONTACT_CREATE_COMMAND"] = "CONTACTINFO",
    ["CONTACT_EDIT_COMMAND"] = "CONTACTINFO",
    ["CONTACT_SET_PRIMARY_COMMAND"] = "CONTACTINFO",
    ["CONTACT_DELETE_COMMAND"] = "CONTACTINFO",
    ["ADDRESS_CREATE_COMMAND"] = "ADDRESSINFO",
    ["ADDRESS_EDIT_COMMAND"] = "ADDRESSINFO",
    ["ADDRESS_SET_PRIMARY_COMMAND"] = "ADDRESSINFO",
    ["ADDRESS_DELETE_COMMAND"] = "ADDRESSINFO",
    ["ORGANIZATION_EDIT_COMMAND"] = "ORGINFO",
  };

  private static readonly Dictionary<string, string> FriendlyNames = new(StringComparer.OrdinalIgnoreCase)
  {
    ["CONTACT_CREATE_COMMAND"] = "contact creation",
    ["CONTACT_EDIT_COMMAND"] = "contact update",
    ["CONTACT_SET_PRIMARY_COMMAND"] = "primary contact change",
    ["CONTACT_DELETE_COMMAND"] = "contact deletion",
    ["ADDRESS_CREATE_COMMAND"] = "address creation",
    ["ADDRESS_EDIT_COMMAND"] = "address update",
    ["ADDRESS_SET_PRIMARY_COMMAND"] = "primary address change",
    ["ADDRESS_DELETE_COMMAND"] = "address deletion",
    ["ORGANIZATION_EDIT_COMMAND"] = "organization update",
  };

  public string? GetCacheSegment(string dataType)
      => CacheSegments.GetValueOrDefault(dataType);

  public string GetFriendlyActionName(string dataType)
      => FriendlyNames.GetValueOrDefault(dataType, "change");

  public CommandPayloadMetadata? ParsePayload(string jsonPayload)
  {
    try
    {
      using var doc = JsonDocument.Parse(jsonPayload);
      var root = doc.RootElement;

      var dataType = TryGetString(root, "dataType") ?? TryGetString(root, "DataType") ?? "UNKNOWN";

      var profileId = Guid.Empty;
      var provider = "UNKNOWN";
      string? entityId = null;

      if (TryGetObject(root, "data", out var data) || TryGetObject(root, "Data", out data))
      {
        if (Guid.TryParse(TryGetString(data, "profileId") ?? TryGetString(data, "ProfileId"), out var pid))
          profileId = pid;

        provider = TryGetString(data, "provider") ?? TryGetString(data, "Provider") ?? "UNKNOWN";

        entityId = TryGetString(data, "contactId") ?? TryGetString(data, "ContactId")
                ?? TryGetString(data, "addressId") ?? TryGetString(data, "AddressId")
                ?? TryGetString(data, "organizationId") ?? TryGetString(data, "OrganizationId");
      }

      return new CommandPayloadMetadata(dataType, profileId, provider, entityId);
    }
    catch
    {
      return null;
    }
  }

  private static string? TryGetString(JsonElement element, string property)
      => element.TryGetProperty(property, out var val) ? val.GetString() : null;

  private static bool TryGetObject(JsonElement element, string property, out JsonElement result)
  {
    if (element.TryGetProperty(property, out result) && result.ValueKind == JsonValueKind.Object)
      return true;
    result = default;
    return false;
  }
}
