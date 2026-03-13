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
    public uint? OrganizationSize { get; init; }
    public string? Sector { get; init; }
    public string? SubSector { get; init; }
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
          NonRegOrgName = "Shrine Org",
          DoingBusinessAs = "DCHF",
          EIN = "12-3456789",
          Founded = 2010,
          FiscalMonth = "Aug",
          FiscalDay = 1,
          OrganizationSize = 50,
          Sector = "Agriculture",
          SubSector = "Livestock",
          Mission = "To improve community health outcomes through innovative programs and partnerships.",
          ServicesAreas = new[] { "Healthcare", "Community Wellness", "Health Education", "Prevention Programs" },
          LastUpdated = DateTime.UtcNow.AddDays(-8),
          AllowEdit = true
        },
        new OrganizationInfo
        {
          Id = "A1B2C3D4-E5F6-7890-ABCD-EF1234567890",
          LastUpdated = DateTime.UtcNow.AddDays(-30)
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
          NonRegOrgName = "Digital Innovation Org",
          DoingBusinessAs = "DETC",
          EIN = "98-7654321",
          Founded = 2015,
          FiscalMonth = "Jul",
          FiscalDay = 23,
          OrganizationSize = 30,
          Sector = "Technology",
          SubSector = "Education",
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

    return new
    {
      Organizations = allOrganizations
        .OrderByDescending(o => o.LastUpdated)
        .Select(o => new
        {
          o.Id,
          o.OrgName,
          o.OrganizationType,
          o.OrgNumber,
          o.OrgStatus,
          o.NonRegOrgName,
          o.FiscalMonth,
          o.FiscalDay,
          o.OrganizationSize,
          o.Sector,
          o.SubSector
        }).ToArray()
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

    return new
    {
      Organizations = allOrganizations
        .OrderByDescending(o => o.LastUpdated)
        .Select(o => new
        {
          o.Id,
          o.OrgName,
          o.OrganizationType,
          o.OrgNumber,
          o.OrgStatus,
          o.NonRegOrgName,
          o.FiscalMonth,
          o.FiscalDay,
          o.OrganizationSize,
          o.Sector,
          o.SubSector
        }).ToArray()
    };
  }

  }
