using Grants.ApplicantPortal.API.Core.DTOs;

namespace Grants.ApplicantPortal.API.Plugins.Demo.Data;

/// <summary>
/// Static data provider for demo organization information with in-memory storage
/// </summary>
public static class OrganizationsData
{
  private static readonly Dictionary<string, List<OrganizationInfo>> _organizationsByProviderProfile = new();
  private static readonly object _lock = new object();

  /// <summary>
  /// Internal organization information structure
  /// </summary>
  private sealed record OrganizationInfo
  {
    public string Id { get; init; } = string.Empty;
    public string OrgName { get; init; } = string.Empty;
    public string OrgNumber { get; init; } = string.Empty;
    public string OrgStatus { get; init; } = string.Empty;
    public string OrganizationType { get; init; } = string.Empty;
    public string LegalName { get; init; } = string.Empty;
    public string NonRegOrgName { get; init; } = string.Empty; // Add NonRegOrgName field
    public string DoingBusinessAs { get; init; } = string.Empty;
    public string EIN { get; init; } = string.Empty;
    public int Founded { get; init; }
    public string FiscalMonth { get; init; } = string.Empty;
    public int FiscalDay { get; init; }
    public string Mission { get; init; } = string.Empty;
    public string[] ServicesAreas { get; init; } = Array.Empty<string>();
    public DateTime LastUpdated { get; init; } = DateTime.UtcNow;
    public bool AllowEdit { get; init; } = true;
    public uint OrganizationSize { get; init; }
  }

  /// <summary>
  /// Gets default organizations for a specific provider
  /// </summary>
  private static OrganizationInfo[] GetDefaultOrganizations(string provider)
  {
    return provider?.ToUpper() switch
    {
      "PROGRAM1" => new[]
      {
        new OrganizationInfo
        {
          Id = "6CEE6704-16C2-4D8B-8575-57D5F25B40D9",
          OrgName = "Cowichan Exhibition",
          OrgNumber = "S0003748",
          OrgStatus = "Active",
          OrganizationType = "Society",
          LegalName = "Demo Community Health Foundation",
          NonRegOrgName = "Shrine Org", // Add default NonRegOrgName
          DoingBusinessAs = "DCHF",
          EIN = "12-3456789",
          Founded = 2010,
          FiscalMonth = "Aug",
          FiscalDay = 1,
          OrganizationSize = 50, // Add default organization size
          Mission = "To improve community health outcomes through innovative programs and partnerships.",
          ServicesAreas = new[] { "Healthcare", "Community Wellness", "Health Education", "Prevention Programs" },
          LastUpdated = DateTime.UtcNow.AddDays(-8),
          AllowEdit = true
        }
      },
      "PROGRAM2" => new[]
      {
        new OrganizationInfo
        {
          Id = "7DEF7815-27D3-5E9C-9686-68E6F36C51EA",
          OrgName = "Hub Tech Solutions",
          OrgNumber = "S1113734",
          OrgStatus = "Active",
          OrganizationType = "Educational Nonprofit",
          LegalName = "Demo Educational Technology Consortium",
          NonRegOrgName = "Digital Innovation Org", // Add default NonRegOrgName
          DoingBusinessAs = "DETC",
          EIN = "98-7654321",
          Founded = 2015,
          FiscalMonth = "Jul",
          FiscalDay = 23,
          OrganizationSize = 30, // Add default organization size
          Mission = "To bridge the digital divide through innovative educational technology solutions and comprehensive training programs.",
          ServicesAreas = new[] { "Educational Technology", "Digital Literacy", "STEM Education", "Teacher Training" },
          LastUpdated = DateTime.UtcNow.AddDays(-6),
          AllowEdit = true
        }
      },
      _ => Array.Empty<OrganizationInfo>()
    };
  }

  /// <summary>
  /// Ensures an organization is materialized into stored organizations if it's a default organization
  /// This is needed when someone tries to manage a default organization
  /// </summary>
  private static void MaterializeDefaultOrganizationIfNeeded(string provider, Guid profileId, string organizationId)
  {
    var key = $"{provider}-{profileId}";
    
    // Debug logging
    System.Diagnostics.Debug.WriteLine($"MaterializeDefaultOrganizationIfNeeded called with OrganizationId: {organizationId}, Provider: {provider}");
    
    if (!_organizationsByProviderProfile.ContainsKey(key))
    {
      _organizationsByProviderProfile[key] = new List<OrganizationInfo>();
    }

    var organizations = _organizationsByProviderProfile[key];
    
    // Check if this organization is already stored (case-insensitive comparison)
    if (organizations.Any(o => string.Equals(o.Id, organizationId, StringComparison.OrdinalIgnoreCase)))
    {
      System.Diagnostics.Debug.WriteLine($"Organization {organizationId} already materialized");
      return; // Already materialized
    }

    // Check if this is a default organization (case-insensitive comparison)
    var defaultOrganizations = GetDefaultOrganizations(provider);
    System.Diagnostics.Debug.WriteLine($"Found {defaultOrganizations.Length} default organizations for provider {provider}");
    foreach (var org in defaultOrganizations)
    {
      System.Diagnostics.Debug.WriteLine($"  Default Organization ID: {org.Id}, Name: {org.OrgName}");
    }
    
    var defaultOrganization = defaultOrganizations.FirstOrDefault(o => string.Equals(o.Id, organizationId, StringComparison.OrdinalIgnoreCase));
    
    if (defaultOrganization != null)
    {
      System.Diagnostics.Debug.WriteLine($"Materializing default organization: {defaultOrganization.OrgName} (ID: {defaultOrganization.Id})");
      // Materialize the default organization into stored organizations
      organizations.Add(defaultOrganization);
    }
    else
    {
      System.Diagnostics.Debug.WriteLine($"No default organization found with ID: {organizationId}");
    }
  }

  /// <summary>
  /// Updates an existing organization
  /// </summary>
  public static bool UpdateOrganization(string provider, Guid profileId, Guid organizationId, EditOrganizationRequest editRequest)
  {
    lock (_lock)
    {
      var key = $"{provider}-{profileId}";

      // Ensure the organization is materialized if it's a default organization
      MaterializeDefaultOrganizationIfNeeded(provider, profileId, organizationId.ToString());

      if (!_organizationsByProviderProfile.ContainsKey(key))
      {
        return false;
      }

      var organizations = _organizationsByProviderProfile[key];
      var organizationIndex = organizations.FindIndex(o => string.Equals(o.Id, organizationId.ToString(), StringComparison.OrdinalIgnoreCase));

      if (organizationIndex == -1)
      {
        return false;
      }

      // Update the organization
      var existingOrganization = organizations[organizationIndex];
      organizations[organizationIndex] = existingOrganization with
      {
        OrgName = editRequest.Name,
        OrganizationType = editRequest.OrganizationType,
        OrgNumber = editRequest.OrganizationNumber,
        OrgStatus = editRequest.Status,
        LegalName = editRequest.LegalName ?? existingOrganization.LegalName,
        NonRegOrgName = editRequest.NonRegOrgName ?? existingOrganization.NonRegOrgName, // Handle NonRegOrgName
        FiscalMonth = editRequest.FiscalMonth ?? existingOrganization.FiscalMonth,
        FiscalDay = editRequest.FiscalDay ?? existingOrganization.FiscalDay,
        OrganizationSize = editRequest.OrganizationSize ?? existingOrganization.OrganizationSize, // Preserve existing value if null
        LastUpdated = DateTime.UtcNow // Always update timestamp when editing
      };

      return true;
    }
  }

  /// <summary>
  /// Gets stored organizations for a provider/profile combination
  /// </summary>
  private static List<OrganizationInfo> GetStoredOrganizations(string provider, Guid profileId)
  {
    lock (_lock)
    {
      var key = $"{provider}-{profileId}";
      return _organizationsByProviderProfile.TryGetValue(key, out var organizations) ? organizations : new List<OrganizationInfo>();
    }
  }
  public static object GenerateProgram1OrgInfo(object baseData)
  {
    // Get the ProfileId from baseData if available
    var profileId = Guid.Empty;
    var baseDataType = baseData.GetType();
    var profileIdProperty = baseDataType.GetProperty("ProfileId");
    if (profileIdProperty != null)
    {
      profileId = (Guid)profileIdProperty.GetValue(baseData)!;
    }

    // Get stored organizations
    var storedOrganizations = GetStoredOrganizations("PROGRAM1", profileId);

    // Default organizations (always present as baseline) - use shared method
    var defaultOrganizations = GetDefaultOrganizations("PROGRAM1");

    // Filter out any default organizations that have been materialized into stored organizations
    // to avoid duplication (case-insensitive comparison)
    var nonMaterializedDefaults = defaultOrganizations.Where(defaultOrg =>
      !storedOrganizations.Any(so => string.Equals(so.Id, defaultOrg.Id, StringComparison.OrdinalIgnoreCase))).ToArray();

    // Combine non-materialized defaults and stored organizations
    var allOrganizations = nonMaterializedDefaults.Concat(storedOrganizations).ToList();

    // Get the first (and typically only) organization
    var organization = allOrganizations.OrderByDescending(o => o.LastUpdated).FirstOrDefault();

    if (organization == null)
    {
      return new
      {
        baseData,
        Data = new
        {
          OrganizationInfo = (object?)null
        }
      };
    }

    return new
    {
      baseData,
      Data = new
      {
        OrganizationInfo = new
        {
          organization.OrgName,
          organization.OrgNumber,
          organization.OrgStatus,
          organization.OrganizationType,
          organization.NonRegOrgName, // Use actual NonRegOrgName from stored data
          OrgSize = organization.OrganizationSize.ToString(), // Use actual organization size
          organization.FiscalMonth,
          organization.FiscalDay,
          OrganizationId = organization.Id,
          organization.LegalName,
          organization.DoingBusinessAs,
          organization.EIN,
          organization.Founded,
          Address = new
          {
            Street = "123 Health Avenue",
            City = "Wellness City",
            State = "BC",
            ZipCode = "V8W 2Y7",
            Country = "Canada"
          },
          ContactInfo = new
          {
            PrimaryContact = new
            {
              Name = "Dr. Sarah Johnson",
              Title = "Executive Director",
              Email = "sarah.johnson@dchf.org",
              Phone = "+1-555-HEALTH"
            },
            GrantsContact = new
            {
              Name = "Michael Chen",
              Title = "Grants Manager",
              Email = "michael.chen@dchf.org",
              Phone = "+1-555-GRANTS"
            }
          },
          organization.Mission,
          organization.ServicesAreas,
          Certifications = new[]
                {
                        new { Type = "CARF Accreditation", ValidUntil = DateTime.UtcNow.AddYears(2) },
                        new { Type = "State Health Department License", ValidUntil = DateTime.UtcNow.AddYears(1) }
                    },
          Program1Specific = new
          {
            EligibilityStatus = "Verified",
            LastAuditDate = DateTime.UtcNow.AddMonths(-6),
            ComplianceScore = 95,
            SpecialDesignations = new[] { "Rural Health Clinic", "FQHC Look-Alike" }
          },
          organization.LastUpdated,
          organization.AllowEdit
        }
      }
    };
  }

  public static object GenerateProgram2OrgInfo(object baseData)
  {
    // Get the ProfileId from baseData if available
    var profileId = Guid.Empty;
    var baseDataType = baseData.GetType();
    var profileIdProperty = baseDataType.GetProperty("ProfileId");
    if (profileIdProperty != null)
    {
      profileId = (Guid)profileIdProperty.GetValue(baseData)!;
    }

    // Get stored organizations
    var storedOrganizations = GetStoredOrganizations("PROGRAM2", profileId);

    // Default organizations (always present as baseline) - use shared method
    var defaultOrganizations = GetDefaultOrganizations("PROGRAM2");

    // Filter out any default organizations that have been materialized into stored organizations
    // to avoid duplication (case-insensitive comparison)
    var nonMaterializedDefaults = defaultOrganizations.Where(defaultOrg =>
      !storedOrganizations.Any(so => string.Equals(so.Id, defaultOrg.Id, StringComparison.OrdinalIgnoreCase))).ToArray();

    // Combine non-materialized defaults and stored organizations
    var allOrganizations = nonMaterializedDefaults.Concat(storedOrganizations).ToList();

    // Get the first (and typically only) organization
    var organization = allOrganizations.OrderByDescending(o => o.LastUpdated).FirstOrDefault();

    if (organization == null)
    {
      return new
      {
        baseData,
        Data = new
        {
          OrganizationInfo = (object?)null
        }
      };
    }

    return new
    {
      baseData,
      Data = new
      {
        OrganizationInfo = new
        {
          organization.OrgName,
          organization.OrgNumber,
          organization.OrgStatus,
          organization.OrganizationType,
          organization.NonRegOrgName, // Use actual NonRegOrgName from stored data
          OrgSize = organization.OrganizationSize.ToString(), // Use actual organization size
          organization.FiscalMonth,
          organization.FiscalDay,
          OrganizationId = organization.Id,
          organization.LegalName,
          organization.DoingBusinessAs,
          organization.EIN,
          organization.Founded,
          Address = new
          {
            Street = "456 Innovation Drive",
            City = "Tech Valley",
            State = "AB",
            ZipCode = "T2P 4K6",
            Country = "Canada"
          },
          ContactInfo = new
          {
            PrimaryContact = new
            {
              Name = "Dr. Maria Rodriguez",
              Title = "Chief Executive Officer",
              Email = "maria.rodriguez@detc.edu",
              Phone = "+1-555-TECH-ED"
            },
            GrantsContact = new
            {
              Name = "James Liu",
              Title = "Director of Development",
              Email = "james.liu@detc.edu",
              Phone = "+1-555-DEV-FUND"
            }
          },
          organization.Mission,
          organization.ServicesAreas,
          Certifications = new[]
                {
                        new { Type = "Department of Education Partnership", ValidUntil = DateTime.UtcNow.AddYears(3) },
                        new { Type = "Technology Integration Certification", ValidUntil = DateTime.UtcNow.AddYears(2) }
                    },
          Program2Specific = new
          {
            EligibilityStatus = "Verified",
            LastTechAudit = DateTime.UtcNow.AddMonths(-3),
            InnovationScore = 88,
            SpecialDesignations = new[] { "STEM Education Hub", "Rural Technology Center" },
            Partnerships = new[] { "State University System", "Tech Industry Coalition", "Rural Education Network" }
          },
          organization.LastUpdated,
          organization.AllowEdit
        }
      }
    };
  }

  public static object GenerateProgram1Payments(object baseData)
  {
    return new
    {
      baseData,
      Data = new
      {
        Payments = new[]
            {
                    new
                    {
                        PaymentId = "PAY-PROG1-001",
                        SubmissionId = "PROG1-SUB-002",
                        ApplicationId = "APP-2024-0045",
                        GrantTitle = "Youth Mental Health Support Program",
                        AwardAmount = 85000,
                        PaymentSchedule = new[]
                        {
                            new
                            {
                                PaymentNumber = 1,
                                Amount = 25500,
                                DueDate = DateTime.UtcNow.AddMonths(1),
                                Status = "Scheduled",
                                Description = "Initial funding - 30%"
                            },
                            new
                            {
                                PaymentNumber = 2,
                                Amount = 29750,
                                DueDate = DateTime.UtcNow.AddMonths(6),
                                Status = "Pending",
                                Description = "Mid-term payment - 35%"
                            },
                            new
                            {
                                PaymentNumber = 3,
                                Amount = 29750,
                                DueDate = DateTime.UtcNow.AddMonths(12),
                                Status = "Pending",
                                Description = "Final payment - 35%"
                            }
                        },
                        PaymentMethod = "Electronic Transfer",
                        BankAccount = "****-****-****-5678",
                        TaxReporting = new
                        {
                            TaxYear = DateTime.UtcNow.Year,
                            Form1099Required = true,
                            ReportingStatus = "Pending"
                        }
                    }
                },
        PaymentSummary = new
        {
          TotalAwardAmount = 85000,
          TotalPaid = 0,
          TotalPending = 85000,
          NextPaymentDue = DateTime.UtcNow.AddMonths(1),
          NextPaymentAmount = 25500
        }
      }
    };
  }
}
