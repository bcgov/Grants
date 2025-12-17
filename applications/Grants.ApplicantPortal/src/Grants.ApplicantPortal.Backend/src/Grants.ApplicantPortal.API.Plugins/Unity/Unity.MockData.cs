using Grants.ApplicantPortal.API.Core.Plugins;

namespace Grants.ApplicantPortal.API.Plugins.Unity;

/// <summary>
/// Mock data generation for Unity plugin when external services are unavailable
/// </summary>
public partial class UnityPlugin
{
    private object GenerateMockData(ProfilePopulationMetadata metadata)
    {
        var baseData = new
        {
            ProfileId = metadata.ProfileId,
            Provider = metadata.Provider,
            Key = metadata.Key,
            Source = "Unity (Mock)",
            PopulatedAt = DateTime.UtcNow,
            PopulatedBy = PluginId,
            IsMockData = true
        };

        return (metadata.Provider?.ToUpper(), metadata.Key?.ToUpper()) switch
        {
            ("UNITY", "PROFILE") => GenerateProfileData(baseData),
            ("UNITY", "EMPLOYMENT") => GenerateEmploymentData(baseData),
            ("UNITY", "SECURITY") => GenerateSecurityData(baseData),
            ("UNITY", "CONTACTS") => GenerateContactsData(baseData),
            ("UNITY", "ADDRESSES") => GenerateAddressesData(baseData),
            ("UNITY", "ORGINFO") => GenerateOrganizationData(baseData),
            _ => GenerateDefaultData(baseData)
        };
    }

    private object GenerateProfileData(object baseData)
    {
        return new
        {
            baseData,
            Data = new
            {
                PersonalInfo = new
                {
                    FirstName = "John",
                    LastName = "Doe",
                    Email = "john.doe@unity.gov",
                    Phone = "+1-555-0123",
                    EmployeeId = "UNI-12345"
                }
            }
        };
    }

    private object GenerateEmploymentData(object baseData)
    {
        return new
        {
            baseData,
            Data = new
            {
                Employment = new
                {
                    Department = "Department of Health",
                    Position = "Senior Analyst",
                    StartDate = "2020-01-15",
                    EmployeeId = "UNI-12345",
                    Manager = "Jane Smith",
                    Location = "Building A, Room 205"
                }
            }
        };
    }

    private object GenerateSecurityData(object baseData)
    {
        return new
        {
            baseData,
            Data = new
            {
                Security = new
                {
                    ClearanceLevel = "Secret",
                    BadgeNumber = "B789456",
                    LastUpdated = DateTime.UtcNow,
                    ExpiryDate = DateTime.UtcNow.AddYears(2),
                    AccessLevel = "Level 3"
                }
            }
        };
    }

    private object GenerateContactsData(object baseData)
    {
        return new
        {
            baseData,
            Data = new
            {
                Contacts = new[]
                {
                    new
                    {
                        ContactId = Guid.NewGuid(),
                        Name = "John Doe",
                        Type = "PRIMARY",
                        IsPrimary = true,
                        Title = "Director",
                        Email = "john.doe@unity.gov",
                        PhoneNumber = "+1-555-0123",
                        LastUpdated = DateTime.UtcNow.AddDays(-5)
                    },
                    new
                    {
                        ContactId = Guid.NewGuid(),
                        Name = "Jane Smith",
                        Type = "GRANTS",
                        IsPrimary = false,
                        Title = "Grants Manager", 
                        Email = "jane.smith@unity.gov",
                        PhoneNumber = "+1-555-0124",
                        LastUpdated = DateTime.UtcNow.AddDays(-2)
                    }
                }
            }
        };
    }

    private object GenerateAddressesData(object baseData)
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
                        AddressId = Guid.NewGuid(),
                        Type = "MAILING",
                        IsPrimary = true,
                        Address = "1234 Government St",
                        City = "Victoria",
                        Province = "BC",
                        PostalCode = "V8W 1A4",
                        Country = "Canada",
                        LastUpdated = DateTime.UtcNow.AddDays(-10)
                    },
                    new
                    {
                        AddressId = Guid.NewGuid(),
                        Type = "PHYSICAL",
                        IsPrimary = false,
                        Address = "5678 Main St",
                        City = "Vancouver",
                        Province = "BC", 
                        PostalCode = "V6B 2C3",
                        Country = "Canada",
                        LastUpdated = DateTime.UtcNow.AddDays(-7)
                    }
                }
            }
        };
    }

    private object GenerateOrganizationData(object baseData)
    {
        return new
        {
            baseData,
            Data = new
            {
                Organization = new
                {
                    OrganizationId = Guid.NewGuid(),
                    Name = "Unity Department",
                    OrganizationType = "Government",
                    LegalName = "Unity Government Department", 
                    Status = "Active",
                    OrganizationNumber = "UNI-ORG-001",
                    Founded = "1995-04-01",
                    Mission = "Serving citizens through innovative government services",
                    ServiceAreas = new[] { "Public Service", "Technology", "Innovation" },
                    LastUpdated = DateTime.UtcNow.AddDays(-1)
                }
            }
        };
    }

    private object GenerateDefaultData(object baseData)
    {
        return new
        {
            baseData,
            Data = new
            {
                Message = "Unity data available for:",
                AvailableProviders = new[] { "Unity" },
                AvailableKeys = new[] { "Profile", "Employment", "Security", "Contacts", "Addresses", "OrgInfo" },
                Instructions = "Use Provider and Key parameters to get specific Unity data",
                Examples = new[]
                {
                    "Provider=Unity, Key=Profile - Get Unity user profile data",
                    "Provider=Unity, Key=Employment - Get Unity employment information",
                    "Provider=Unity, Key=Security - Get Unity security clearance data",
                    "Provider=Unity, Key=Contacts - Get Unity contact information",
                    "Provider=Unity, Key=Addresses - Get Unity address information",
                    "Provider=Unity, Key=OrgInfo - Get Unity organization information"
                }
            }
        };
    }
}
