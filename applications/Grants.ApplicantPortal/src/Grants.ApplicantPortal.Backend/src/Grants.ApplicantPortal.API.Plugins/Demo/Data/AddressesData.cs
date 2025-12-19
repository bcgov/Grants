using Grants.ApplicantPortal.API.Core.DTOs;

namespace Grants.ApplicantPortal.API.Plugins.Demo.Data;

/// <summary>
/// Static data provider for demo address information with in-memory storage
/// </summary>
public static class AddressesData
{
  private static readonly Dictionary<string, List<AddressInfo>> _addressesByProviderProfile = new();
  private static readonly object _lock = new object();

  /// <summary>
  /// Internal address information structure
  /// </summary>
  private record AddressInfo
  {
    public string Id { get; init; } = string.Empty;
    public string AddressId { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public string AddressLine1 { get; init; } = string.Empty;
    public string AddressLine2 { get; init; } = string.Empty;
    public string Street { get; init; } = string.Empty;
    public string City { get; init; } = string.Empty;
    public string Province { get; init; } = string.Empty;
    public string PostalCode { get; init; } = string.Empty;
    public string Country { get; init; } = string.Empty;
    public bool IsPrimary { get; init; }
    public bool IsActive { get; init; } = true;
    public DateTime LastVerified { get; init; } = DateTime.UtcNow;
    public bool AllowEdit { get; init; } = true;
  }

  /// <summary>
  /// Gets default addresses for a specific provider
  /// </summary>
  private static AddressInfo[] GetDefaultAddresses(string provider)
  {
    return provider?.ToUpper() switch
    {
      "PROGRAM1" => new[]
      {
        new AddressInfo
        {
          Id = "AD12E345-6789-0ABC-DEF1-234567890ABC",
          AddressId = "ADDR-P1-001",
          Type = "Physical",
          AddressLine1 = "123 Main Street",
          AddressLine2 = "Suite 100",
          Street = "123 Main Street, Suite 100",
          City = "Vancouver",
          Province = "BC",
          PostalCode = "V6B 1A1",
          Country = "Canada",
          IsPrimary = true,
          IsActive = true,
          LastVerified = DateTime.UtcNow.AddDays(-10),
          AllowEdit = true
        },
        new AddressInfo
        {
          Id = "BD12E345-6789-0ABC-DEF1-234567890ABC",
          AddressId = "ADDR-P1-002",
          Type = "Mailing",
          AddressLine1 = "456 Business Ave",
          AddressLine2 = "",
          Street = "456 Business Ave",
          City = "Victoria",
          Province = "BC", 
          PostalCode = "V8W 2Y7",
          Country = "Canada",
          IsPrimary = false,
          IsActive = true,
          LastVerified = DateTime.UtcNow.AddDays(-7),
          AllowEdit = true
        },
        new AddressInfo
        {
          Id = "CD12E345-6789-0ABC-DEF1-234567890ABC",
          AddressId = "ADDR-P1-003",
          Type = "Mailing",
          AddressLine1 = "PO Box 789",
          AddressLine2 = "",
          Street = "PO Box 789",
          City = "Burnaby",
          Province = "BC", 
          PostalCode = "V5H 3Z4",
          Country = "Canada",
          IsPrimary = false,
          IsActive = true,
          LastVerified = DateTime.UtcNow.AddDays(-4),
          AllowEdit = true
        }
      },
      "PROGRAM2" => new[]
      {
        new AddressInfo
        {
          Id = "DD12E345-6789-0ABC-DEF1-234567890ABC",
          AddressId = "ADDR-P2-001",
          Type = "Physical",
          AddressLine1 = "456 Innovation Drive",
          AddressLine2 = "Building A",
          Street = "456 Innovation Drive, Building A",
          City = "Tech Valley",
          Province = "AB",
          PostalCode = "T2P 4K6",
          Country = "Canada",
          IsPrimary = true,
          IsActive = true,
          LastVerified = DateTime.UtcNow.AddDays(-9)
        },
        new AddressInfo
        {
          Id = "ED12E345-6789-0ABC-DEF1-234567890ABC",
          AddressId = "ADDR-P2-002",
          Type = "Mailing",
          AddressLine1 = "789 Research Blvd",
          AddressLine2 = "Suite 200",
          Street = "789 Research Blvd, Suite 200",
          City = "Innovation City",
          Province = "AB",
          PostalCode = "T2P 4K7",
          Country = "Canada",
          IsPrimary = false,
          IsActive = true,
          LastVerified = DateTime.UtcNow.AddDays(-6)
        },
        new AddressInfo
        {
          Id = "FD12E345-6789-0ABC-DEF1-234567890ABC",
          AddressId = "ADDR-P2-003",
          Type = "Mailing",
          AddressLine1 = "PO Box 1234",
          AddressLine2 = "",
          Street = "PO Box 1234",
          City = "Calgary",
          Province = "AB",
          PostalCode = "T2G 5L8",
          Country = "Canada",
          IsPrimary = false,
          IsActive = true,
          LastVerified = DateTime.UtcNow.AddDays(-3)
        }
      },
      _ => Array.Empty<AddressInfo>()
    };
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
      System.Diagnostics.Debug.WriteLine($"  Default Address ID: {da.Id}, Type: {da.Type}");
    }
    
    var defaultAddress = defaultAddresses.FirstOrDefault(a => string.Equals(a.Id, addressId, StringComparison.OrdinalIgnoreCase));
    
    if (defaultAddress != null)
    {
      System.Diagnostics.Debug.WriteLine($"Materializing default address: {defaultAddress.Type} (ID: {defaultAddress.Id})");
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

      // If this is being set as primary, update other stored addresses to not be primary
      if (editRequest.IsPrimary)
      {
        for (var i = 0; i < addresses.Count; i++)
        {
          if (i != addressIndex)
          {
            addresses[i] = addresses[i] with { IsPrimary = false };
          }
        }
      }

      // Update the address
      var existingAddress = addresses[addressIndex];
      addresses[addressIndex] = existingAddress with
      {
        Type = editRequest.Type,
        AddressLine1 = editRequest.Address,
        Street = editRequest.Address,
        City = editRequest.City,
        Province = editRequest.Province,
        PostalCode = editRequest.PostalCode,
        Country = editRequest.Country ?? "Canada",
        IsPrimary = editRequest.IsPrimary,
        LastVerified = DateTime.UtcNow // Always update timestamp when editing
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
        System.Diagnostics.Debug.WriteLine($"  Address ID: {address.Id}, Type: {address.Type}");
      }
      
      var addressIndex = addresses.FindIndex(a => string.Equals(a.Id, addressId.ToString(), StringComparison.OrdinalIgnoreCase));
      
      if (addressIndex == -1)
      {
        System.Diagnostics.Debug.WriteLine($"Address with ID {addressId} not found in stored addresses");
        return false;
      }

      System.Diagnostics.Debug.WriteLine($"Found address at index {addressIndex}, setting as primary");

      // Update all stored addresses to not be primary, then set the target as primary
      for (var i = 0; i < addresses.Count; i++)
      {
        addresses[i] = addresses[i] with 
        { 
          IsPrimary = i == addressIndex,
          LastVerified = i == addressIndex ? DateTime.UtcNow : addresses[i].LastVerified // Update timestamp for primary address
        };
      }

      return true;
    }
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
    
    // Handle primary address conflicts - only one can be primary
    var primaryAddresses = allAddresses.Where(a => a.IsPrimary).ToList();
    if (primaryAddresses.Count > 1)
    {
      // If there are multiple primary addresses, prefer stored addresses over default addresses
      // and if there are multiple stored primary addresses, keep the most recent one
      var storedPrimary = primaryAddresses.Where(a => storedAddresses.Contains(a)).OrderByDescending(a => a.LastVerified).FirstOrDefault();
      
      if (storedPrimary != null)
      {
        // Set stored address as primary and make all others non-primary
        for (var i = 0; i < allAddresses.Count; i++)
        {
          if (allAddresses[i].Id == storedPrimary.Id)
          {
            allAddresses[i] = allAddresses[i] with { IsPrimary = true };
          }
          else if (allAddresses[i].IsPrimary)
          {
            allAddresses[i] = allAddresses[i] with { IsPrimary = false };
          }
        }
      }
    }

    return new
    {
      baseData,
      Data = new
      {
        Addresses = allAddresses
          .OrderByDescending(a => a.LastVerified) // Most recently updated first
          .Select(a => new
          {
            a.Id,
            a.AddressId,
            a.Type,
            a.AddressLine1,
            a.AddressLine2,
            a.Street,
            a.City,
            a.Province,
            a.PostalCode,
            a.Country,
            a.IsPrimary,
            a.IsActive,
            a.LastVerified,
            a.AllowEdit
          }).ToArray(),
        Summary = new
        {
          TotalAddresses = allAddresses.Count,
          PrimaryAddressCount = allAddresses.Count(a => a.IsPrimary),
          ActiveAddressCount = allAddresses.Count(a => a.IsActive)
        }
      }
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
    
    // Handle primary address conflicts - only one can be primary
    var primaryAddresses = allAddresses.Where(a => a.IsPrimary).ToList();
    if (primaryAddresses.Count > 1)
    {
      // If there are multiple primary addresses, prefer stored addresses over default addresses
      // and if there are multiple stored primary addresses, keep the most recent one
      var storedPrimary = primaryAddresses.Where(a => storedAddresses.Contains(a)).OrderByDescending(a => a.LastVerified).FirstOrDefault();
      
      if (storedPrimary != null)
      {
        // Set stored address as primary and make all others non-primary
        for (var i = 0; i < allAddresses.Count; i++)
        {
          if (allAddresses[i].Id == storedPrimary.Id)
          {
            allAddresses[i] = allAddresses[i] with { IsPrimary = true };
          }
          else if (allAddresses[i].IsPrimary)
          {
            allAddresses[i] = allAddresses[i] with { IsPrimary = false };
          }
        }
      }
    }

    return new
    {
      baseData,
      Data = new
      {
        Addresses = allAddresses
          .OrderByDescending(a => a.LastVerified) // Most recently updated first
          .Select(a => new
          {
            a.Id,
            a.AddressId,
            a.Type,
            a.AddressLine1,
            a.AddressLine2,
            a.Street,
            a.City,
            a.Province,
            a.PostalCode,
            a.Country,
            a.IsPrimary,
            a.IsActive,
            a.LastVerified,
            AllowEdit = true // All stored addresses are editable; default addresses have their own AllowEdit setting
          }).ToArray(),
        Summary = new
        {
          TotalAddresses = allAddresses.Count,
          PrimaryAddressCount = allAddresses.Count(a => a.IsPrimary),
          ActiveAddressCount = allAddresses.Count(a => a.IsActive)
        }
      }
    };
  }
}
