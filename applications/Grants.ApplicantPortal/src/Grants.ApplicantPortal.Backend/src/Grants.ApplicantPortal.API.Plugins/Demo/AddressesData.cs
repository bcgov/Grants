namespace Grants.ApplicantPortal.API.Plugins.Demo;

/// <summary>
/// Static data provider for demo address information
/// </summary>
public static class AddressesData
{
  public static object GenerateProgram1Addresses(object baseData)
  {
    return new
    {
      baseData,
      Data = new
      {
        Addresses = new[]
        {
          new
          {
            Id = "1",
            AddressId = "ADDR-P1-001",
            Type = "Primary",
            AddressLine1 = "123 Main Street",
            AddressLine2 = "Suite 100",
            Street = "123 Main Street, Suite 100",
            City = "Vancouver",
            Province = "BC",
            PostalCode = "V6B 1A1",
            Country = "Canada",
            IsPrimary = true,
            IsActive = true,
            LastVerified = DateTime.UtcNow.AddMonths(-1)
          },
          new
          {
            Id = "2",
            AddressId = "ADDR-P1-002",
            Type = "Billing",
            AddressLine1 = "456 Business Ave",
            AddressLine2 = "",
            Street = "456 Business Ave",
            City = "Victoria",
            Province = "BC", 
            PostalCode = "V8W 2Y7",
            Country = "Canada",
            IsPrimary = false,
            IsActive = true,
            LastVerified = DateTime.UtcNow.AddMonths(-2)
          },
          new
          {
            Id = "3",
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
            LastVerified = DateTime.UtcNow.AddMonths(-1)
          }
        },
        Summary = new
        {
          TotalAddresses = 3,
          PrimaryAddressCount = 1,
          ActiveAddressCount = 3
        }
      }
    };
  }

  public static object GenerateProgram2Addresses(object baseData)
  {
    return new
    {
      baseData,
      Data = new
      {
        Addresses = new[]
        {
          new
          {
            Id = "1",
            AddressId = "ADDR-P2-001",
            Type = "Primary",
            AddressLine1 = "456 Innovation Drive",
            AddressLine2 = "Building A",
            Street = "456 Innovation Drive, Building A",
            City = "Tech Valley",
            Province = "AB",
            PostalCode = "T2P 4K6",
            Country = "Canada",
            IsPrimary = true,
            IsActive = true,
            LastVerified = DateTime.UtcNow.AddMonths(-1)
          },
          new
          {
            Id = "2",
            AddressId = "ADDR-P2-002",
            Type = "Billing",
            AddressLine1 = "789 Research Blvd",
            AddressLine2 = "Suite 200",
            Street = "789 Research Blvd, Suite 200",
            City = "Innovation City",
            Province = "AB",
            PostalCode = "T2P 4K7",
            Country = "Canada",
            IsPrimary = false,
            IsActive = true,
            LastVerified = DateTime.UtcNow.AddMonths(-1)
          },
          new
          {
            Id = "3",
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
            LastVerified = DateTime.UtcNow.AddMonths(-2)
          }
        },
        Summary = new
        {
          TotalAddresses = 3,
          PrimaryAddressCount = 1,
          ActiveAddressCount = 3
        }
      }
    };
  }
}
