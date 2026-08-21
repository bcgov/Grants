using Grants.ApplicantPortal.API.Core.DTOs;
using Grants.ApplicantPortal.API.Core.Plugins;

namespace Grants.ApplicantPortal.API.Plugins.Demo.Data;

/// <summary>
/// Static data provider for demo address information with in-memory storage
/// </summary>
public static class AddressesData
{
  private static readonly Dictionary<string, List<AddressInfo>> _addressesByProviderProfile = new();
  private static readonly object _lock = new object();

  /// <summary>
  /// Internal address information structure aligned with real Unity API response
  /// </summary>
  private sealed record AddressInfo
  {
    public string Id { get; init; } = string.Empty;
    public string AddressType { get; init; } = string.Empty;
    public string Street { get; init; } = string.Empty;
    public string Street2 { get; init; } = string.Empty;
    public string Unit { get; init; } = string.Empty;
    public string City { get; init; } = string.Empty;
    public string Province { get; init; } = string.Empty;
    public string PostalCode { get; init; } = string.Empty;
    public string Country { get; init; } = string.Empty;
    public bool IsPrimary { get; init; }
    public bool IsEditable { get; init; }
    public string ReferenceNo { get; init; } = string.Empty;
  }

  /// <summary>
  /// Gets default addresses for a specific provider
  /// </summary>
  private static AddressInfo[] GetDefaultAddresses(string provider)
  {
    return provider?.ToUpper() switch
    {
      "PROGRAM1" =>
      [
        new AddressInfo
        {
          Id = "AD12E345-6789-0ABC-DEF1-234567890ABC",
          AddressType = "Physical",
          Street = "123 Main Street",
          Street2 = "Suite 100",
          Unit = "",
          City = "Vancouver",
          Province = "BC",
          PostalCode = "V6B1A1",
          Country = "",
          IsPrimary = true,
          IsEditable = false,
          ReferenceNo = "DEMO0001"
        },
        new AddressInfo
        {
          Id = "BD12E345-6789-0ABC-DEF1-234567890ABC",
          AddressType = "Mailing",
          Street = "456 Business Ave",
          Street2 = "",
          Unit = "",
          City = "Victoria",
          Province = "BC",
          PostalCode = "V8W2Y7",
          Country = "",
          IsPrimary = false,
          IsEditable = false,
          ReferenceNo = "DEMO0001"
        },
        new AddressInfo
        {
          Id = "CD12E345-6789-0ABC-DEF1-234567890ABC",
          AddressType = "Mailing",
          Street = "PO Box 789",
          Street2 = "",
          Unit = "",
          City = "Burnaby",
          Province = "BC",
          PostalCode = "V5H3Z4",
          Country = "",
          IsPrimary = false,
          IsEditable = false,
          ReferenceNo = "DEMO0002"
        }
      ],
      "PROGRAM2" =>
      [
        new AddressInfo
        {
          Id = "DD12E345-6789-0ABC-DEF1-234567890ABC",
          AddressType = "Physical",
          Street = "456 Innovation Drive",
          Street2 = "Building A",
          Unit = "",
          City = "Tech Valley",
          Province = "AB",
          PostalCode = "T2P4K6",
          Country = "",
          IsPrimary = true,
          IsEditable = false,
          ReferenceNo = "DEMO0003"
        },
        new AddressInfo
        {
          Id = "ED12E345-6789-0ABC-DEF1-234567890ABC",
          AddressType = "Mailing",
          Street = "789 Research Blvd",
          Street2 = "Suite 200",
          Unit = "",
          City = "Innovation City",
          Province = "AB",
          PostalCode = "T2P4K7",
          Country = "",
          IsPrimary = false,
          IsEditable = false,
          ReferenceNo = "DEMO0003"
        },
        new AddressInfo
        {
          Id = "FD12E345-6789-0ABC-DEF1-234567890ABC",
          AddressType = "Mailing",
          Street = "PO Box 1234",
          Street2 = "",
          Unit = "",
          City = "Calgary",
          Province = "AB",
          PostalCode = "T2G5L8",
          Country = "",
          IsPrimary = false,
          IsEditable = false,
          ReferenceNo = "DEMO0004"
        }
      ],
      _ => Array.Empty<AddressInfo>()
    };
  }

  /// <summary>
  /// Adds a new address to the in-memory store
  /// </summary>
  public static string AddAddress(string provider, Guid profileId, CreateAddressRequest addressRequest)
  {
    lock (_lock)
    {
      var key = $"{provider}-{profileId}";

      if (!_addressesByProviderProfile.ContainsKey(key))
      {
        _addressesByProviderProfile[key] = new List<AddressInfo>();
      }

      var addresses = _addressesByProviderProfile[key];

      // If this is being set as primary, only demote the existing stored addresses of the SAME
      // address type — every other address type keeps its own primary address.
      if (addressRequest.IsPrimary)
      {
        for (var i = 0; i < addresses.Count; i++)
        {
          if (IsSameAddressType(addresses[i].AddressType, addressRequest.AddressType))
          {
            addresses[i] = addresses[i] with { IsPrimary = false };
          }
        }
      }

      // Generate a new address ID
      var newAddressId = Guid.NewGuid();

      var newAddress = new AddressInfo
      {
        Id = newAddressId.ToString(),
        AddressType = addressRequest.AddressType,
        Street = addressRequest.Street,
        Street2 = addressRequest.Street2 ?? "",
        Unit = addressRequest.Unit ?? "",
        City = addressRequest.City,
        Province = addressRequest.Province,
        PostalCode = addressRequest.PostalCode,
        Country = addressRequest.Country ?? "",
        IsPrimary = addressRequest.IsPrimary,
        IsEditable = true,
        ReferenceNo = ""
      };

      addresses.Add(newAddress);

      return newAddressId.ToString();
    }
  }

  /// <summary>
  /// Ensures an address is materialized into stored addresses if it's a default address
  /// This is needed when someone tries to manage a default address
  /// </summary>
  private static void MaterializeDefaultAddressIfNeeded(string provider, Guid profileId, string addressId)
  {
    var key = $"{provider}-{profileId}";
    
    // Debug logging
    System.Diagnostics.Debug.WriteLine($"MaterializeDefaultAddressIfNeeded called with AddressId: {addressId}, Provider: {provider}");
    
    if (!_addressesByProviderProfile.ContainsKey(key))
    {
      _addressesByProviderProfile[key] = new List<AddressInfo>();
    }

    var addresses = _addressesByProviderProfile[key];
    
    // Check if this address is already stored (case-insensitive comparison)
    if (addresses.Any(a => string.Equals(a.Id, addressId, StringComparison.OrdinalIgnoreCase)))
    {
      System.Diagnostics.Debug.WriteLine($"Address {addressId} already materialized");
      return; // Already materialized
    }

    // Check if this is a default address (case-insensitive comparison)
    var defaultAddresses = GetDefaultAddresses(provider);
    System.Diagnostics.Debug.WriteLine($"Found {defaultAddresses.Length} default addresses for provider {provider}");
    foreach (var da in defaultAddresses)
    {
      System.Diagnostics.Debug.WriteLine($"  Default Address ID: {da.Id}, AddressType: {da.AddressType}");
    }
    
    var defaultAddress = defaultAddresses.FirstOrDefault(a => string.Equals(a.Id, addressId, StringComparison.OrdinalIgnoreCase));
    
    if (defaultAddress != null)
    {
      System.Diagnostics.Debug.WriteLine($"Materializing default address: {defaultAddress.AddressType} (ID: {defaultAddress.Id})");
      // Materialize the default address into stored addresses
      addresses.Add(defaultAddress);
    }
    else
    {
      System.Diagnostics.Debug.WriteLine($"No default address found with ID: {addressId}");
    }
  }

  /// <summary>
  /// Updates an existing address
  /// </summary>
  public static bool UpdateAddress(string provider, Guid profileId, Guid addressId, EditAddressRequest editRequest)
  {
    lock (_lock)
    {
      var key = $"{provider}-{profileId}";
      
      // Ensure the address is materialized if it's a default address
      MaterializeDefaultAddressIfNeeded(provider, profileId, addressId.ToString());
      
      if (!_addressesByProviderProfile.ContainsKey(key))
      {
        return false;
      }

      var addresses = _addressesByProviderProfile[key];
      var addressIndex = addresses.FindIndex(a => string.Equals(a.Id, addressId.ToString(), StringComparison.OrdinalIgnoreCase));
      
      if (addressIndex == -1)
      {
        return false;
      }

      // Read the type the address had BEFORE the update so a type change can be detected.
      // When the type changes the address leaves its old type group behind; that group then has
      // no flagged primary and one is inferred when the addresses are next read.
      var previousAddressType = addresses[addressIndex].AddressType;

      // If this is being set as primary, only demote the other stored addresses that share the
      // address type the edited address is being saved with.
      if (editRequest.IsPrimary)
      {
        for (var i = 0; i < addresses.Count; i++)
        {
          if (i != addressIndex && IsSameAddressType(addresses[i].AddressType, editRequest.AddressType))
          {
            addresses[i] = addresses[i] with { IsPrimary = false };
          }
        }
      }

      System.Diagnostics.Debug.WriteLine(
        $"UpdateAddress {addressId}: type {previousAddressType} -> {editRequest.AddressType}, isPrimary {editRequest.IsPrimary}");

      // Update the address
      var existingAddress = addresses[addressIndex];
      addresses[addressIndex] = existingAddress with
      {
        AddressType = editRequest.AddressType,
        Street = editRequest.Street,
        Street2 = editRequest.Street2 ?? "",
        Unit = editRequest.Unit ?? "",
        City = editRequest.City,
        Province = editRequest.Province,
        PostalCode = editRequest.PostalCode,
        Country = editRequest.Country ?? "",
        IsPrimary = editRequest.IsPrimary
      };

      return true;
    }
  }

  /// <summary>
  /// Sets an address as primary
  /// </summary>
  public static bool SetAddressAsPrimary(string provider, Guid profileId, Guid addressId)
  {
    lock (_lock)
    {
      var key = $"{provider}-{profileId}";
      
      // Debug logging
      System.Diagnostics.Debug.WriteLine($"SetAddressAsPrimary called with AddressId: {addressId}, Provider: {provider}, ProfileId: {profileId}");
      
      // Ensure the address is materialized if it's a default address
      MaterializeDefaultAddressIfNeeded(provider, profileId, addressId.ToString());
      
      if (!_addressesByProviderProfile.ContainsKey(key))
      {
        System.Diagnostics.Debug.WriteLine($"No stored addresses found for key: {key}");
        return false;
      }

      var addresses = _addressesByProviderProfile[key];
      System.Diagnostics.Debug.WriteLine($"Found {addresses.Count} stored addresses");
      foreach (var address in addresses)
      {
        System.Diagnostics.Debug.WriteLine($"  Address ID: {address.Id}, AddressType: {address.AddressType}");
      }
      
      var addressIndex = addresses.FindIndex(a => string.Equals(a.Id, addressId.ToString(), StringComparison.OrdinalIgnoreCase));
      
      if (addressIndex == -1)
      {
        System.Diagnostics.Debug.WriteLine($"Address with ID {addressId} not found in stored addresses");
        return false;
      }

      System.Diagnostics.Debug.WriteLine($"Found address at index {addressIndex}, setting as primary");

      // The address type is derived from the target address itself — only that type group is
      // re-flagged, so setting e.g. a Mailing address as primary leaves the Physical primary alone.
      var targetAddressType = addresses[addressIndex].AddressType;

      for (var i = 0; i < addresses.Count; i++)
      {
        if (i == addressIndex)
        {
          addresses[i] = addresses[i] with { IsPrimary = true };
        }
        else if (IsSameAddressType(addresses[i].AddressType, targetAddressType))
        {
          addresses[i] = addresses[i] with { IsPrimary = false };
        }
      }

      return true;
    }
  }

  /// <summary>
  /// Deletes an address from the in-memory store
  /// </summary>
  public static bool DeleteAddress(string provider, Guid profileId, Guid addressId)
  {
    lock (_lock)
    {
      var key = $"{provider}-{profileId}";
      var addressIdStr = addressId.ToString();

      // Ensure the address is materialized if it's a default address
      MaterializeDefaultAddressIfNeeded(provider, profileId, addressIdStr);

      if (!_addressesByProviderProfile.ContainsKey(key))
      {
        return false;
      }

      var addresses = _addressesByProviderProfile[key];
      var addressIndex = addresses.FindIndex(a => string.Equals(a.Id, addressIdStr, StringComparison.OrdinalIgnoreCase));

      if (addressIndex == -1)
      {
        return false;
      }

      var wasPrimary = addresses[addressIndex].IsPrimary;
      var deletedAddressType = addresses[addressIndex].AddressType;
      addresses.RemoveAt(addressIndex);

      // If we deleted a primary address, promote the first remaining address OF THE SAME TYPE.
      // Other address types keep the primary they already have.
      if (wasPrimary)
      {
        var promoteIndex = addresses.FindIndex(a => IsSameAddressType(a.AddressType, deletedAddressType));
        if (promoteIndex >= 0)
        {
          addresses[promoteIndex] = addresses[promoteIndex] with { IsPrimary = true };
        }
      }

      return true;
    }
  }

  /// <summary>
  /// Compares two address types. Types are compared case-insensitively and never hardcoded;
  /// a missing type is normalized so untyped addresses still form one consistent group.
  /// </summary>
  private static bool IsSameAddressType(string? left, string? right)
    => AddressTypeKey.AreSame(left, right);

  /// <summary>
  /// Guarantees exactly one primary address per address type.
  /// Within a type group the stored (applicant-managed) address wins over a default demo address;
  /// when a group has no flagged primary at all, one is promoted so every type resolves to a primary.
  /// </summary>
  private static List<AddressInfo> ResolveOnePrimaryPerType(
    List<AddressInfo> allAddresses,
    List<AddressInfo> storedAddresses)
  {
    var primaryIdByType = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    foreach (var group in allAddresses.GroupBy(a => a.AddressType?.Trim() ?? string.Empty, StringComparer.OrdinalIgnoreCase))
    {
      var members = group.ToList();
      if (members.Count == 0)
      {
        continue;
      }

      var flagged = members.Where(a => a.IsPrimary).ToList();
      var candidates = flagged.Count > 0 ? flagged : members;

      // Prefer a stored address over a default one when resolving the conflict
      var winner = candidates.FirstOrDefault(c =>
                     storedAddresses.Any(s => string.Equals(s.Id, c.Id, StringComparison.OrdinalIgnoreCase)))
                   ?? candidates[0];

      primaryIdByType[group.Key] = winner.Id;
    }

    return [.. allAddresses
      .Select(a => a with
      {
        IsPrimary = primaryIdByType.TryGetValue(a.AddressType?.Trim() ?? string.Empty, out var primaryId) &&
                    string.Equals(a.Id, primaryId, StringComparison.OrdinalIgnoreCase)
      })];
  }

  /// <summary>
  /// Gets stored addresses for a provider/profile combination
  /// </summary>
  private static List<AddressInfo> GetStoredAddresses(string provider, Guid profileId)
  {
    lock (_lock)
    {
      var key = $"{provider}-{profileId}";
      return _addressesByProviderProfile.TryGetValue(key, out var addresses) ? addresses : new List<AddressInfo>();
    }
  }
  public static object GenerateProgram1Addresses(object baseData)
  {
    // Get the ProfileId from baseData if available
    var profileId = Guid.Empty;
    var baseDataType = baseData.GetType();
    var profileIdProperty = baseDataType.GetProperty("ProfileId");
    if (profileIdProperty != null)
    {
      profileId = (Guid)profileIdProperty.GetValue(baseData)!;
    }

    // Get stored addresses
    var storedAddresses = GetStoredAddresses("PROGRAM1", profileId);

    // Default addresses (always present as baseline) - use shared method
    var defaultAddresses = GetDefaultAddresses("PROGRAM1");
    
    // Filter out any default addresses that have been materialized into stored addresses
    // to avoid duplication (case-insensitive comparison)
    var nonMaterializedDefaults = defaultAddresses.Where(da => 
      !storedAddresses.Any(sa => string.Equals(sa.Id, da.Id, StringComparison.OrdinalIgnoreCase))).ToArray();

    // Combine non-materialized defaults and stored addresses
    var allAddresses = nonMaterializedDefaults.Concat(storedAddresses).ToList();

    // Handle primary address conflicts - exactly one primary per address type
    allAddresses = ResolveOnePrimaryPerType(allAddresses, storedAddresses);

    return new
    {
      Addresses = allAddresses
        .Select(a => new
        {
          a.Id,
          a.AddressType,
          a.Street,
          a.Street2,
          a.Unit,
          a.City,
          a.Province,
          a.PostalCode,
          a.Country,
          a.IsPrimary,
          a.IsEditable,
          a.ReferenceNo
        }).ToArray()
    };
  }

  public static object GenerateProgram2Addresses(object baseData)
  {
    // Get the ProfileId from baseData if available
    var profileId = Guid.Empty;
    var baseDataType = baseData.GetType();
    var profileIdProperty = baseDataType.GetProperty("ProfileId");
    if (profileIdProperty != null)
    {
      profileId = (Guid)profileIdProperty.GetValue(baseData)!;
    }

    // Get stored addresses
    var storedAddresses = GetStoredAddresses("PROGRAM2", profileId);

    // Default addresses (always present as baseline) - use shared method
    var defaultAddresses = GetDefaultAddresses("PROGRAM2");
    
    // Filter out any default addresses that have been materialized into stored addresses
    // to avoid duplication (case-insensitive comparison)
    var nonMaterializedDefaults = defaultAddresses.Where(da => 
      !storedAddresses.Any(sa => string.Equals(sa.Id, da.Id, StringComparison.OrdinalIgnoreCase))).ToArray();

    // Combine non-materialized defaults and stored addresses
    var allAddresses = nonMaterializedDefaults.Concat(storedAddresses).ToList();

    // Handle primary address conflicts - exactly one primary per address type
    allAddresses = ResolveOnePrimaryPerType(allAddresses, storedAddresses);

    return new
    {
      Addresses = allAddresses
        .Select(a => new
        {
          a.Id,
          a.AddressType,
          a.Street,
          a.Street2,
          a.Unit,
          a.City,
          a.Province,
          a.PostalCode,
          a.Country,
          a.IsPrimary,
          a.IsEditable,
          a.ReferenceNo
        }).ToArray()
    };
  }
}
