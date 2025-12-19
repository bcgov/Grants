using Grants.ApplicantPortal.API.Core.DTOs;

namespace Grants.ApplicantPortal.API.Plugins.Demo.Data;

/// <summary>
/// Static data provider for demo contact information with in-memory storage
/// </summary>
public static class ContactsData
{
  private static readonly Dictionary<string, List<ContactInfo>> _contactsByProviderProfile = new();
  private static readonly Dictionary<string, HashSet<string>> _deletedDefaultContactIds = new();
  private static readonly object _lock = new object();

  /// <summary>
  /// Internal contact information structure
  /// </summary>
  private sealed record ContactInfo
  {
    public string Id { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Phone { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public bool IsPrimary { get; init; }
    public bool IsActive { get; init; } = true;
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
        for (var i = 0; i < contacts.Count; i++)
        {
          contacts[i] = contacts[i] with { IsPrimary = false };
        }
      }

      // Generate a new contact ID
      var newContactId = Guid.NewGuid();

      var newContact = new ContactInfo
      {
        Id = newContactId.ToString(),
        Type = contactRequest.Type ?? "Secondary",
        Name = contactRequest.Name ?? "",
        Email = contactRequest.Email ?? "",
        Phone = contactRequest.PhoneNumber ?? "",
        Title = contactRequest.Title ?? "",
        IsPrimary = contactRequest.IsPrimary,
        IsActive = true,
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
      var contactIndex = contacts.FindIndex(c => string.Equals(c.Id, contactId.ToString(), StringComparison.OrdinalIgnoreCase));
      
      if (contactIndex == -1)
      {
        return false;
      }

      // If this is being set as primary, update other stored contacts to not be primary
      // Note: We only manage primary status within stored contacts; default contacts 
      // will be handled in the generation methods
      if (editRequest.IsPrimary)
      {
        for (var i = 0; i < contacts.Count; i++)
        {
          if (i != contactIndex)
          {
            contacts[i] = contacts[i] with { IsPrimary = false };
          }
        }
      }

      // Update the contact
      var existingContact = contacts[contactIndex];
      contacts[contactIndex] = existingContact with
      {
        Type = editRequest.Type,
        Name = editRequest.Name ?? "",
        Email = editRequest.Email ?? "",
        Phone = editRequest.PhoneNumber ?? "",
        Title = editRequest.Title ?? "",
        IsPrimary = editRequest.IsPrimary,
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
      
      // Debug logging
      System.Diagnostics.Debug.WriteLine($"SetContactAsPrimary called with ContactId: {contactId}, Provider: {provider}, ProfileId: {profileId}");
      
      // Ensure the contact is materialized if it's a default contact
      MaterializeDefaultContactIfNeeded(provider, profileId, contactId.ToString());
      
      if (!_contactsByProviderProfile.ContainsKey(key))
      {
        System.Diagnostics.Debug.WriteLine($"No stored contacts found for key: {key}");
        return false;
      }

      var contacts = _contactsByProviderProfile[key];
      System.Diagnostics.Debug.WriteLine($"Found {contacts.Count} stored contacts");
      foreach (var contact in contacts)
      {
        System.Diagnostics.Debug.WriteLine($"  Contact ID: {contact.Id}, Name: {contact.Name}");
      }
      
      var contactIndex = contacts.FindIndex(c => string.Equals(c.Id, contactId.ToString(), StringComparison.OrdinalIgnoreCase));
      
      if (contactIndex == -1)
      {
        System.Diagnostics.Debug.WriteLine($"Contact with ID {contactId} not found in stored contacts");
        return false;
      }

      System.Diagnostics.Debug.WriteLine($"Found contact at index {contactIndex}, setting as primary");

      // Update all stored contacts to not be primary, then set the target as primary
      // Note: We only manage primary status within stored contacts; default contacts 
      // will be handled in the generation methods
      for (var i = 0; i < contacts.Count; i++)
      {
        contacts[i] = contacts[i] with 
        { 
          IsPrimary = i == contactIndex,
          LastUpdated = i == contactIndex ? DateTime.UtcNow : contacts[i].LastUpdated // Update timestamp for primary contact
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
      var contactIdStr = contactId.ToString();
      
      // Check if it's a default contact
      var defaultContacts = GetDefaultContacts(provider);
      var isDefaultContact = defaultContacts.Any(c => string.Equals(c.Id, contactIdStr, StringComparison.OrdinalIgnoreCase));
      
      var wasPrimary = false;
      
      if (isDefaultContact)
      {
        // Check if the default contact being deleted is primary
        var defaultContact = defaultContacts.FirstOrDefault(c => string.Equals(c.Id, contactIdStr, StringComparison.OrdinalIgnoreCase));
        wasPrimary = defaultContact?.IsPrimary ?? false;
        
        // Track deletion of default contact
        if (!_deletedDefaultContactIds.ContainsKey(key))
        {
          _deletedDefaultContactIds[key] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }
        _deletedDefaultContactIds[key].Add(contactIdStr);
        
        // Also remove from stored contacts if it was materialized
        if (_contactsByProviderProfile.ContainsKey(key))
        {
          var contacts = _contactsByProviderProfile[key];
          var contactIndex = contacts.FindIndex(c => string.Equals(c.Id, contactIdStr, StringComparison.OrdinalIgnoreCase));
          if (contactIndex != -1)
          {
            wasPrimary = contacts[contactIndex].IsPrimary;
            contacts.RemoveAt(contactIndex);
          }
        }
      }
      else
      {
        // Handle stored contacts
        if (!_contactsByProviderProfile.ContainsKey(key))
        {
          return false;
        }

        var storedContacts = _contactsByProviderProfile[key];
        var storedContactIndex = storedContacts.FindIndex(c => string.Equals(c.Id, contactIdStr, StringComparison.OrdinalIgnoreCase));
        
        if (storedContactIndex == -1)
        {
          return false;
        }

        wasPrimary = storedContacts[storedContactIndex].IsPrimary;
        storedContacts.RemoveAt(storedContactIndex);
      }
      
      // If we deleted a primary contact, find the next contact to make primary
      if (wasPrimary)
      {
        PromoteNextContactToPrimary(provider, profileId);
      }
      
      return true;
    }
  }

  /// <summary>
  /// Promotes the most recently modified contact to primary when the current primary is deleted
  /// </summary>
  private static void PromoteNextContactToPrimary(string provider, Guid profileId)
  {
    var key = $"{provider}-{profileId}";
    
    // Get all remaining contacts (stored + non-deleted defaults)
    var storedContacts = GetStoredContacts(provider, profileId);
    var defaultContacts = GetDefaultContacts(provider);
    var deletedDefaultIds = _deletedDefaultContactIds.TryGetValue(key, out var deletedIds) ? deletedIds : new HashSet<string>();
    
    // Get non-deleted, non-materialized default contacts
    var nonMaterializedDefaults = defaultContacts.Where(dc => 
      !storedContacts.Any(sc => string.Equals(sc.Id, dc.Id, StringComparison.OrdinalIgnoreCase)) &&
      !deletedDefaultIds.Contains(dc.Id)).ToArray();
    
    var allContacts = nonMaterializedDefaults.Concat(storedContacts).ToList();
    
    if (allContacts.Count == 0)
    {
      return; // No contacts left
    }
    
    // Find the most recently modified contact
    var newPrimaryContact = allContacts.OrderByDescending(c => c.LastUpdated).First();
    
    // If it's a default contact, we need to materialize it first
    if (nonMaterializedDefaults.Any(c => c.Id == newPrimaryContact.Id))
    {
      MaterializeDefaultContactIfNeeded(provider, profileId, newPrimaryContact.Id);
      
      // Get the updated stored contacts after materialization
      storedContacts = GetStoredContacts(provider, profileId);
    }
    
    // Set the new primary contact in stored contacts
    if (_contactsByProviderProfile.ContainsKey(key))
    {
      var contacts = _contactsByProviderProfile[key];
      var contactIndex = contacts.FindIndex(c => string.Equals(c.Id, newPrimaryContact.Id, StringComparison.OrdinalIgnoreCase));
      
      if (contactIndex != -1)
      {
        contacts[contactIndex] = contacts[contactIndex] with 
        { 
          IsPrimary = true, 
          LastUpdated = DateTime.UtcNow 
        };
      }
    }
  }

  /// <summary>
  /// Gets default contacts for a specific provider
  /// </summary>
  private static ContactInfo[] GetDefaultContacts(string provider)
  {
    return provider?.ToUpper() switch
    {
      "PROGRAM1" =>
      [
        new ContactInfo
        {
          Id = "437675A8-D642-455C-B3E0-388D75E6203F",
          Type = "Primary",
          Name = "John Doe",
          Email = "john.doe@example.com",
          Phone = "(555) 123-4567",
          Title = "Project Manager",
          IsPrimary = true,
          IsActive = true,
          LastUpdated = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc), // Fixed timestamp
          AllowEdit = true
        },
        new ContactInfo
        {
          Id = "5A017931-E247-48C7-8257-25B0ED239883",
          Type = "Secondary",
          Name = "Jane Smith",
          Email = "jane.smith@example.com",
          Phone = "(555) 987-6543",
          Title = "Financial Officer",
          IsPrimary = false,
          IsActive = true,
          LastUpdated = new DateTime(2024, 1, 1, 12, 10, 0, DateTimeKind.Utc), // Fixed timestamp
          AllowEdit = false
        }
      ],
      "PROGRAM2" => new[]
      {
        new ContactInfo
        {
          Id = "127675A8-D653-455C-B3E0-388D75E6203F",
          Type = "Primary",
          Name = "Dr. Maria Rodriguez",
          Email = "maria.rodriguez@detc.edu",
          Phone = "+1-555-TECH-ED",
          Title = "Chief Executive Officer",
          IsPrimary = true,
          IsActive = true,
          LastUpdated = new DateTime(2024, 1, 1, 12, 20, 0, DateTimeKind.Utc), // Fixed timestamp
          AllowEdit = false
        },
        new ContactInfo
        {
          Id = "6B017931-E247-48C7-8257-25B0ED239872",
          Type = "Secondary",
          Name = "James Liu",
          Email = "james.liu@detc.edu",
          Phone = "+1-555-DEV-FUND",
          Title = "Director of Development",
          IsPrimary = false,
          IsActive = true,
          LastUpdated = new DateTime(2024, 1, 1, 12, 30, 0, DateTimeKind.Utc), // Fixed timestamp
          AllowEdit = true
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
    
    // Debug logging
    System.Diagnostics.Debug.WriteLine($"MaterializeDefaultContactIfNeeded called with ContactId: {contactId}, Provider: {provider}");
    
    if (!_contactsByProviderProfile.ContainsKey(key))
    {
      _contactsByProviderProfile[key] = new List<ContactInfo>();
    }

    var contacts = _contactsByProviderProfile[key];
    
    // Check if this contact is already stored (case-insensitive comparison)
    if (contacts.Any(c => string.Equals(c.Id, contactId, StringComparison.OrdinalIgnoreCase)))
    {
      System.Diagnostics.Debug.WriteLine($"Contact {contactId} already materialized");
      return; // Already materialized
    }

    // Check if this is a default contact (case-insensitive comparison)
    var defaultContacts = GetDefaultContacts(provider);
    System.Diagnostics.Debug.WriteLine($"Found {defaultContacts.Length} default contacts for provider {provider}");
    foreach (var dc in defaultContacts)
    {
      System.Diagnostics.Debug.WriteLine($"  Default Contact ID: {dc.Id}, Name: {dc.Name}");
    }
    
    var defaultContact = defaultContacts.FirstOrDefault(c => string.Equals(c.Id, contactId, StringComparison.OrdinalIgnoreCase));
    
    if (defaultContact != null)
    {
      System.Diagnostics.Debug.WriteLine($"Materializing default contact: {defaultContact.Name} (ID: {defaultContact.Id})");
      // Materialize the default contact into stored contacts
      contacts.Add(defaultContact);
    }
    else
    {
      System.Diagnostics.Debug.WriteLine($"No default contact found with ID: {contactId}");
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
    
    // Get deleted default contact IDs for this provider/profile
    var key = $"PROGRAM1-{profileId}";
    var deletedDefaultIds = _deletedDefaultContactIds.TryGetValue(key, out var deletedIds) ? deletedIds : new HashSet<string>();
    
    // Filter out any default contacts that have been materialized into stored contacts
    // or have been deleted (case-insensitive comparison)
    var nonMaterializedDefaults = defaultContacts.Where(dc => 
      !storedContacts.Any(sc => string.Equals(sc.Id, dc.Id, StringComparison.OrdinalIgnoreCase)) &&
      !deletedDefaultIds.Contains(dc.Id)).ToArray();

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
        for (var i = 0; i < allContacts.Count; i++)
        {
          if (allContacts[i].Id == storedPrimary.Id)
          {
            allContacts[i] = allContacts[i] with { IsPrimary = true };
          }
          else if (allContacts[i].IsPrimary)
          {
            allContacts[i] = allContacts[i] with { IsPrimary = false };
          }
        }
      }
    }

    return new
    {
      baseData,
      Data = new
      {
        Contacts = allContacts
          .OrderByDescending(c => c.LastUpdated) // Most recently updated first
          .Select(c => new
          {
            c.Id,
            c.Type,
            c.Name,
            c.Email,
            c.Phone,
            c.Title,
            c.IsPrimary,
            c.IsActive,
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
    
    // Get deleted default contact IDs for this provider/profile
    var key = $"PROGRAM2-{profileId}";
    var deletedDefaultIds = _deletedDefaultContactIds.TryGetValue(key, out var deletedIds) ? deletedIds : new HashSet<string>();
    
    // Filter out any default contacts that have been materialized into stored contacts
    // or have been deleted (case-insensitive comparison)
    var nonMaterializedDefaults = defaultContacts.Where(dc => 
      !storedContacts.Any(sc => string.Equals(sc.Id, dc.Id, StringComparison.OrdinalIgnoreCase)) &&
      !deletedDefaultIds.Contains(dc.Id)).ToArray();

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
        for (var i = 0; i < allContacts.Count; i++)
        {
          if (allContacts[i].Id == storedPrimary.Id)
          {
            allContacts[i] = allContacts[i] with { IsPrimary = true };
          }
          else if (allContacts[i].IsPrimary)
          {
            allContacts[i] = allContacts[i] with { IsPrimary = false };
          }
        }
      }
    }

    return new
    {
      baseData,
      Data = new
      {
        Contacts = allContacts
          .OrderByDescending(c => c.LastUpdated) // Most recently updated first
          .Select(c => new
          {
            c.Id,
            c.Type,
            c.Name,
            c.Email,
            c.Phone,
            c.Title,
            c.IsPrimary,
            c.IsActive,
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
