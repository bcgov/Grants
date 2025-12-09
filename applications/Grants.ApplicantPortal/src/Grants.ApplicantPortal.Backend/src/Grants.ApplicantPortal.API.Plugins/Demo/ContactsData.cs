using Grants.ApplicantPortal.API.Core.DTOs;

namespace Grants.ApplicantPortal.API.Plugins.Demo;

/// <summary>
/// Static data provider for demo contact information with in-memory storage
/// </summary>
public static class ContactsData
{
  private static readonly Dictionary<string, List<ContactInfo>> _contactsByProviderProfile = new();
  private static readonly object _lock = new object();

  /// <summary>
  /// Internal contact information structure
  /// </summary>
  private record ContactInfo
  {
    public string Id { get; init; } = string.Empty;
    public string ContactId { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Phone { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Extension { get; init; } = string.Empty;
    public string Department { get; init; } = string.Empty;
    public bool IsPrimary { get; init; }
    public bool IsActive { get; init; } = true;
    public bool PreferredContact { get; init; }
    public DateTime LastUpdated { get; init; } = DateTime.UtcNow;
    public bool AllowEdit { get; init; } = true;
  }

  /// <summary>
  /// Adds a new contact to the in-memory store
  /// </summary>
  public static string AddContact(string provider, Guid profileId, CreateContactRequest contactRequest)
  {
    lock (_lock)
    {
      var key = $"{provider}-{profileId}";
      
      if (!_contactsByProviderProfile.ContainsKey(key))
      {
        _contactsByProviderProfile[key] = new List<ContactInfo>();
      }

      var contacts = _contactsByProviderProfile[key];
      
      // If this is being set as primary, update existing stored contacts to not be primary
      // Note: We only manage primary status within stored contacts; default contacts 
      // will be handled in the generation methods
      if (contactRequest.IsPrimary)
      {
        for (int i = 0; i < contacts.Count; i++)
        {
          contacts[i] = contacts[i] with { IsPrimary = false, PreferredContact = false };
        }
      }

      // Generate a new contact ID
      var newContactId = Guid.NewGuid();
      var contactId = $"CON-{provider.ToUpper()}-{contacts.Count + 1:D3}";

      // Parse name into first and last name
      var nameParts = contactRequest.Name?.Split(' ', 2) ?? new[] { "", "" };
      var firstName = nameParts.Length > 0 ? nameParts[0] : "";
      var lastName = nameParts.Length > 1 ? nameParts[1] : "";

      var newContact = new ContactInfo
      {
        Id = newContactId.ToString(),
        ContactId = contactId,
        Type = contactRequest.Type ?? "Secondary",
        FirstName = firstName,
        LastName = lastName,
        Name = contactRequest.Name ?? "",
        Email = contactRequest.Email ?? "",
        Phone = contactRequest.PhoneNumber ?? "",
        Title = contactRequest.Title ?? "",
        Extension = "",
        Department = "",
        IsPrimary = contactRequest.IsPrimary,
        IsActive = true,
        PreferredContact = contactRequest.IsPrimary,
        LastUpdated = DateTime.UtcNow,
        AllowEdit = true
      };

      contacts.Add(newContact);
      
      return newContactId.ToString();
    }
  }

  /// <summary>
  /// Updates an existing contact
  /// </summary>
  public static bool UpdateContact(string provider, Guid profileId, Guid contactId, EditContactRequest editRequest)
  {
    lock (_lock)
    {
      var key = $"{provider}-{profileId}";
      
      // Ensure the contact is materialized if it's a default contact
      MaterializeDefaultContactIfNeeded(provider, profileId, contactId.ToString());
      
      if (!_contactsByProviderProfile.ContainsKey(key))
      {
        return false;
      }

      var contacts = _contactsByProviderProfile[key];
      var contactIndex = contacts.FindIndex(c => c.Id == contactId.ToString());
      
      if (contactIndex == -1)
      {
        return false;
      }

      // If this is being set as primary, update other stored contacts to not be primary
      // Note: We only manage primary status within stored contacts; default contacts 
      // will be handled in the generation methods
      if (editRequest.IsPrimary)
      {
        for (int i = 0; i < contacts.Count; i++)
        {
          if (i != contactIndex)
          {
            contacts[i] = contacts[i] with { IsPrimary = false, PreferredContact = false };
          }
        }
      }

      // Parse name into first and last name
      var nameParts = editRequest.Name?.Split(' ', 2) ?? new[] { "", "" };
      var firstName = nameParts.Length > 0 ? nameParts[0] : "";
      var lastName = nameParts.Length > 1 ? nameParts[1] : "";

      // Update the contact
      var existingContact = contacts[contactIndex];
      contacts[contactIndex] = existingContact with
      {
        Type = editRequest.Type,
        FirstName = firstName,
        LastName = lastName,
        Name = editRequest.Name,
        Email = editRequest.Email ?? "",
        Phone = editRequest.PhoneNumber ?? "",
        Title = editRequest.Title ?? "",
        IsPrimary = editRequest.IsPrimary,
        PreferredContact = editRequest.IsPrimary,
        LastUpdated = DateTime.UtcNow
      };

      return true;
    }
  }

  /// <summary>
  /// Sets a contact as primary
  /// </summary>
  public static bool SetContactAsPrimary(string provider, Guid profileId, Guid contactId)
  {
    lock (_lock)
    {
      var key = $"{provider}-{profileId}";
      
      // Ensure the contact is materialized if it's a default contact
      MaterializeDefaultContactIfNeeded(provider, profileId, contactId.ToString());
      
      if (!_contactsByProviderProfile.ContainsKey(key))
      {
        return false;
      }

      var contacts = _contactsByProviderProfile[key];
      var contactIndex = contacts.FindIndex(c => c.Id == contactId.ToString());
      
      if (contactIndex == -1)
      {
        return false;
      }

      // Update all stored contacts to not be primary, then set the target as primary
      // Note: We only manage primary status within stored contacts; default contacts 
      // will be handled in the generation methods
      for (int i = 0; i < contacts.Count; i++)
      {
        contacts[i] = contacts[i] with 
        { 
          IsPrimary = i == contactIndex,
          PreferredContact = i == contactIndex
        };
      }

      return true;
    }
  }

  /// <summary>
  /// Deletes a contact
  /// </summary>
  public static bool DeleteContact(string provider, Guid profileId, Guid contactId)
  {
    lock (_lock)
    {
      var key = $"{provider}-{profileId}";
      
      // Ensure the contact is materialized if it's a default contact
      MaterializeDefaultContactIfNeeded(provider, profileId, contactId.ToString());
      
      if (!_contactsByProviderProfile.ContainsKey(key))
      {
        return false;
      }

      var contacts = _contactsByProviderProfile[key];
      var contactIndex = contacts.FindIndex(c => c.Id == contactId.ToString());
      
      if (contactIndex == -1)
      {
        return false;
      }

      contacts.RemoveAt(contactIndex);
      return true;
    }
  }

  /// <summary>
  /// Gets default contacts for a specific provider
  /// </summary>
  private static ContactInfo[] GetDefaultContacts(string provider)
  {
    return provider?.ToUpper() switch
    {
      "PROGRAM1" => new[]
      {
        new ContactInfo
        {
          Id = "437675A8-D642-455C-B3E0-388D75E6203F",
          ContactId = "CON-P1-001",
          Type = "Primary",
          FirstName = "John",
          LastName = "Doe",
          Name = "John Doe",
          Email = "john.doe@example.com",
          Phone = "(555) 123-4567",
          Title = "Project Manager",
          Extension = "101",
          Department = "Administration",
          IsPrimary = true,
          IsActive = true,
          PreferredContact = true,
          LastUpdated = DateTime.UtcNow.AddDays(-5),
          AllowEdit = true
        },
        new ContactInfo
        {
          Id = "5A017931-E247-48C7-8257-25B0ED239883",
          ContactId = "CON-P1-002", 
          Type = "Secondary",
          FirstName = "Jane",
          LastName = "Smith",
          Name = "Jane Smith",
          Email = "jane.smith@example.com",
          Phone = "(555) 987-6543",
          Title = "Financial Officer",
          Extension = "205",
          Department = "Finance",
          IsPrimary = false,
          IsActive = true,
          PreferredContact = false,
          LastUpdated = DateTime.UtcNow.AddDays(-2),
          AllowEdit = true
        }
      },
      "PROGRAM2" => new[]
      {
        new ContactInfo
        {
          Id = "1",
          ContactId = "CON-P2-001",
          Type = "Primary",
          FirstName = "Dr. Maria",
          LastName = "Rodriguez",
          Name = "Dr. Maria Rodriguez",
          Email = "maria.rodriguez@detc.edu",
          Phone = "+1-555-TECH-ED",
          Title = "Chief Executive Officer",
          Extension = "100",
          Department = "Executive",
          IsPrimary = true,
          IsActive = true,
          PreferredContact = true,
          LastUpdated = DateTime.UtcNow.AddDays(-3)
        },
        new ContactInfo
        {
          Id = "2",
          ContactId = "CON-P2-002",
          Type = "Secondary",
          FirstName = "James",
          LastName = "Liu",
          Name = "James Liu",
          Email = "james.liu@detc.edu",
          Phone = "+1-555-DEV-FUND",
          Title = "Director of Development",
          Extension = "150",
          Department = "Development",
          IsPrimary = false,
          IsActive = true,
          PreferredContact = false,
          LastUpdated = DateTime.UtcNow.AddDays(-1)
        }
      },
      _ => Array.Empty<ContactInfo>()
    };
  }

  /// <summary>
  /// Ensures a contact is materialized into stored contacts if it's a default contact
  /// This is needed when someone tries to manage a default contact
  /// </summary>
  private static void MaterializeDefaultContactIfNeeded(string provider, Guid profileId, string contactId)
  {
    var key = $"{provider}-{profileId}";
    
    if (!_contactsByProviderProfile.ContainsKey(key))
    {
      _contactsByProviderProfile[key] = new List<ContactInfo>();
    }

    var contacts = _contactsByProviderProfile[key];
    
    // Check if this contact is already stored
    if (contacts.Any(c => c.Id == contactId))
    {
      return; // Already materialized
    }

    // Check if this is a default contact
    var defaultContacts = GetDefaultContacts(provider);
    var defaultContact = defaultContacts.FirstOrDefault(c => c.Id == contactId);
    
    if (defaultContact != null)
    {
      // Materialize the default contact into stored contacts
      contacts.Add(defaultContact);
    }
  }

  /// <summary>
  /// Gets stored contacts for a provider/profile combination
  /// </summary>
  private static List<ContactInfo> GetStoredContacts(string provider, Guid profileId)
  {
    lock (_lock)
    {
      var key = $"{provider}-{profileId}";
      return _contactsByProviderProfile.TryGetValue(key, out var contacts) ? contacts : new List<ContactInfo>();
    }
  }

  public static object GenerateProgram1Contacts(object baseData)
  {
    // Get the ProfileId from baseData if available
    var profileId = Guid.Empty;
    var baseDataType = baseData.GetType();
    var profileIdProperty = baseDataType.GetProperty("ProfileId");
    if (profileIdProperty != null)
    {
      profileId = (Guid)profileIdProperty.GetValue(baseData)!;
    }

    // Get stored contacts
    var storedContacts = GetStoredContacts("PROGRAM1", profileId);

    // Default contacts (always present as baseline) - use shared method
    var defaultContacts = GetDefaultContacts("PROGRAM1");
    
    // Filter out any default contacts that have been materialized into stored contacts
    // to avoid duplication
    var nonMaterializedDefaults = defaultContacts.Where(dc => 
      !storedContacts.Any(sc => sc.Id == dc.Id)).ToArray();

    // Combine non-materialized defaults and stored contacts
    var allContacts = nonMaterializedDefaults.Concat(storedContacts).ToList();
    
    // Handle primary contact conflicts - only one can be primary
    var primaryContacts = allContacts.Where(c => c.IsPrimary).ToList();
    if (primaryContacts.Count > 1)
    {
      // If there are multiple primary contacts, prefer stored contacts over default contacts
      // and if there are multiple stored primary contacts, keep the most recent one
      var storedPrimary = primaryContacts.Where(c => storedContacts.Contains(c)).OrderByDescending(c => c.LastUpdated).FirstOrDefault();
      
      if (storedPrimary != null)
      {
        // Set stored contact as primary and make all others non-primary
        for (int i = 0; i < allContacts.Count; i++)
        {
          if (allContacts[i].Id == storedPrimary.Id)
          {
            allContacts[i] = allContacts[i] with { IsPrimary = true, PreferredContact = true };
          }
          else if (allContacts[i].IsPrimary)
          {
            allContacts[i] = allContacts[i] with { IsPrimary = false, PreferredContact = false };
          }
        }
      }
    }

    return new
    {
      baseData,
      Data = new
      {
        Contacts = allContacts.Select(c => new
        {
          c.Id,
          c.ContactId,
          c.Type,
          c.FirstName,
          c.LastName,
          c.Name,
          c.Email,
          c.Phone,
          c.Title,
          c.Extension,
          c.Department,
          c.IsPrimary,
          c.IsActive,
          c.PreferredContact,
          c.LastUpdated,
          c.AllowEdit
        }).ToArray(),
        Summary = new
        {
          TotalContacts = allContacts.Count,
          PrimaryContactCount = allContacts.Count(c => c.IsPrimary),
          ActiveContactCount = allContacts.Count(c => c.IsActive)
        }
      }
    };
  }

  public static object GenerateProgram2Contacts(object baseData)
  {
    // Get the ProfileId from baseData if available
    var profileId = Guid.Empty;
    var baseDataType = baseData.GetType();
    var profileIdProperty = baseDataType.GetProperty("ProfileId");
    if (profileIdProperty != null)
    {
      profileId = (Guid)profileIdProperty.GetValue(baseData)!;
    }

    // Get stored contacts
    var storedContacts = GetStoredContacts("PROGRAM2", profileId);

    // Default contacts (always present as baseline) - use shared method
    var defaultContacts = GetDefaultContacts("PROGRAM2");
    
    // Filter out any default contacts that have been materialized into stored contacts
    // to avoid duplication
    var nonMaterializedDefaults = defaultContacts.Where(dc => 
      !storedContacts.Any(sc => sc.Id == dc.Id)).ToArray();

    // Combine non-materialized defaults and stored contacts
    var allContacts = nonMaterializedDefaults.Concat(storedContacts).ToList();
    
    // Handle primary contact conflicts - only one can be primary
    var primaryContacts = allContacts.Where(c => c.IsPrimary).ToList();
    if (primaryContacts.Count > 1)
    {
      // If there are multiple primary contacts, prefer stored contacts over default contacts
      // and if there are multiple stored primary contacts, keep the most recent one
      var storedPrimary = primaryContacts.Where(c => storedContacts.Contains(c)).OrderByDescending(c => c.LastUpdated).FirstOrDefault();
      
      if (storedPrimary != null)
      {
        // Set stored contact as primary and make all others non-primary
        for (int i = 0; i < allContacts.Count; i++)
        {
          if (allContacts[i].Id == storedPrimary.Id)
          {
            allContacts[i] = allContacts[i] with { IsPrimary = true, PreferredContact = true };
          }
          else if (allContacts[i].IsPrimary)
          {
            allContacts[i] = allContacts[i] with { IsPrimary = false, PreferredContact = false };
          }
        }
      }
    }

    return new
    {
      baseData,
      Data = new
      {
        Contacts = allContacts.Select(c => new
        {
          c.Id,
          c.ContactId,
          c.Type,
          c.FirstName,
          c.LastName,
          c.Name,
          c.Email,
          c.Phone,
          c.Title,
          c.Extension,
          c.Department,
          c.IsPrimary,
          c.IsActive,
          c.PreferredContact,
          c.LastUpdated,
          AllowEdit = true // All stored contacts are editable; default contacts have their own AllowEdit setting
        }).ToArray(),
        Summary = new
        {
          TotalContacts = allContacts.Count,
          PrimaryContactCount = allContacts.Count(c => c.IsPrimary),
          ActiveContactCount = allContacts.Count(c => c.IsActive)
        }
      }
    };
  }
}
