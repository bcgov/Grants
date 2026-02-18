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
  /// Internal contact information structure matching Unity API schema
  /// </summary>
  private sealed record ContactInfo
  {
    public string ContactId { get; init; } = string.Empty;
    public string ContactType { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string? HomePhoneNumber { get; init; }
    public string? MobilePhoneNumber { get; init; }
    public string? WorkPhoneNumber { get; init; }
    public string? WorkPhoneExtension { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Role { get; init; }
    public bool IsPrimary { get; init; }
    public bool IsEditable { get; init; } = true;
    public Guid? ApplicationId { get; init; }
    public DateTime LastUpdated { get; init; } = DateTime.UtcNow;
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
        ContactId = newContactId.ToString(),
        ContactType = contactRequest.ContactType ?? "ApplicantProfile",
        Name = contactRequest.Name ?? "",
        Email = contactRequest.Email ?? "",
        HomePhoneNumber = contactRequest.HomePhoneNumber,
        MobilePhoneNumber = contactRequest.MobilePhoneNumber,
        WorkPhoneNumber = contactRequest.WorkPhoneNumber,
        WorkPhoneExtension = contactRequest.WorkPhoneExtension,
        Title = contactRequest.Title ?? "",
        Role = contactRequest.Role,
        IsPrimary = contactRequest.IsPrimary,
        IsEditable = true,
        ApplicationId = null,
        LastUpdated = DateTime.UtcNow
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
      var contactIndex = contacts.FindIndex(c => string.Equals(c.ContactId, contactId.ToString(), StringComparison.OrdinalIgnoreCase));

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

      // Update the contact - preserve existing values when incoming fields are null
      var existingContact = contacts[contactIndex];
      contacts[contactIndex] = existingContact with
      {
        ContactType = editRequest.ContactType,
        Name = editRequest.Name ?? existingContact.Name,
        Email = editRequest.Email ?? existingContact.Email,
        HomePhoneNumber = editRequest.HomePhoneNumber ?? existingContact.HomePhoneNumber,
        MobilePhoneNumber = editRequest.MobilePhoneNumber ?? existingContact.MobilePhoneNumber,
        WorkPhoneNumber = editRequest.WorkPhoneNumber ?? existingContact.WorkPhoneNumber,
        WorkPhoneExtension = editRequest.WorkPhoneExtension ?? existingContact.WorkPhoneExtension,
        Title = editRequest.Title ?? existingContact.Title,
        Role = editRequest.Role ?? existingContact.Role,
        IsPrimary = editRequest.IsPrimary,
        LastUpdated = DateTime.UtcNow
      };

      return true;
    }
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
    if (contacts.Any(c => string.Equals(c.ContactId, contactId, StringComparison.OrdinalIgnoreCase)))
    {
      System.Diagnostics.Debug.WriteLine($"Contact {contactId} already materialized");
      return; // Already materialized
    }

    // Check if this is a default contact (case-insensitive comparison)
    var defaultContacts = GetDefaultContacts(provider);
    System.Diagnostics.Debug.WriteLine($"Found {defaultContacts.Length} default contacts for provider {provider}");
    foreach (var dc in defaultContacts)
    {
      System.Diagnostics.Debug.WriteLine($"  Default Contact ID: {dc.ContactId}, Name: {dc.Name}");
    }

    var defaultContact = defaultContacts.FirstOrDefault(c => string.Equals(c.ContactId, contactId, StringComparison.OrdinalIgnoreCase));

    if (defaultContact != null)
    {
      System.Diagnostics.Debug.WriteLine($"Materializing default contact: {defaultContact.Name} (ID: {defaultContact.ContactId})");
      // Materialize the default contact into stored contacts
      contacts.Add(defaultContact);
    }
    else
    {
      System.Diagnostics.Debug.WriteLine($"No default contact found with ID: {contactId}");
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
        System.Diagnostics.Debug.WriteLine($"  Contact ID: {contact.ContactId}, Name: {contact.Name}");
      }

      var contactIndex = contacts.FindIndex(c => string.Equals(c.ContactId, contactId.ToString(), StringComparison.OrdinalIgnoreCase));

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
      var isDefaultContact = defaultContacts.Any(c => string.Equals(c.ContactId, contactIdStr, StringComparison.OrdinalIgnoreCase));

      var wasPrimary = false;

      if (isDefaultContact)
      {
        // Check if the default contact being deleted is primary
        var defaultContact = defaultContacts.FirstOrDefault(c => string.Equals(c.ContactId, contactIdStr, StringComparison.OrdinalIgnoreCase));
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
          var contactIndex = contacts.FindIndex(c => string.Equals(c.ContactId, contactIdStr, StringComparison.OrdinalIgnoreCase));
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
        var storedContactIndex = storedContacts.FindIndex(c => string.Equals(c.ContactId, contactIdStr, StringComparison.OrdinalIgnoreCase));

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
      !storedContacts.Any(sc => string.Equals(sc.ContactId, dc.ContactId, StringComparison.OrdinalIgnoreCase)) &&
      !deletedDefaultIds.Contains(dc.ContactId)).ToArray();

    var allContacts = nonMaterializedDefaults.Concat(storedContacts).ToList();

    if (allContacts.Count == 0)
    {
      return; // No contacts left
    }

    // Find the most recently modified contact
    var newPrimaryContact = allContacts.OrderByDescending(c => c.LastUpdated).First();

    // If it's a default contact, we need to materialize it first
    if (nonMaterializedDefaults.Any(c => c.ContactId == newPrimaryContact.ContactId))
    {
      MaterializeDefaultContactIfNeeded(provider, profileId, newPrimaryContact.ContactId);

      // Get the updated stored contacts after materialization
      storedContacts = GetStoredContacts(provider, profileId);
    }

    // Set the new primary contact in stored contacts
    if (_contactsByProviderProfile.ContainsKey(key))
    {
      var contacts = _contactsByProviderProfile[key];
      var contactIndex = contacts.FindIndex(c => string.Equals(c.ContactId, newPrimaryContact.ContactId, StringComparison.OrdinalIgnoreCase));

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
          ContactId = "437675A8-D642-455C-B3E0-388D75E6203F",
          ContactType = "ApplicantProfile",
          Name = "John Doe",
          Email = "john.doe@example.com",
          HomePhoneNumber = "(555) 123-4567",
          MobilePhoneNumber = null,
          WorkPhoneNumber = "(555) 123-9999",
          WorkPhoneExtension = "101",
          Title = "Project Manager",
          Role = "primary",
          IsPrimary = true,
          IsEditable = true,
          ApplicationId = null,
          LastUpdated = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc)
        },
        new ContactInfo
        {
          ContactId = "5A017931-E247-48C7-8257-25B0ED239883",
          ContactType = "Application",
          Name = "Jane Smith",
          Email = "jane.smith@example.com",
          HomePhoneNumber = null,
          MobilePhoneNumber = null,
          WorkPhoneNumber = null,
          WorkPhoneExtension = null,
          Title = "Financial Officer",
          Role = "Additional Signing Authority",
          IsPrimary = false,
          IsEditable = false,
          ApplicationId = Guid.Parse("3a1eac9f-da13-a888-883f-d2c0575e7620"),
          LastUpdated = new DateTime(2024, 1, 1, 12, 10, 0, DateTimeKind.Utc)
        }
      ],
      "PROGRAM2" => new[]
      {
        new ContactInfo
        {
          ContactId = "127675A8-D653-455C-B3E0-388D75E6203F",
          ContactType = "ApplicantProfile",
          Name = "Dr. Maria Rodriguez",
          Email = "maria.rodriguez@detc.edu",
          HomePhoneNumber = null,
          MobilePhoneNumber = "+1-555-832-4333",
          WorkPhoneNumber = "+1-555-832-4300",
          WorkPhoneExtension = null,
          Title = "Chief Executive Officer",
          Role = "ceo",
          IsPrimary = true,
          IsEditable = false,
          ApplicationId = null,
          LastUpdated = new DateTime(2024, 1, 1, 12, 20, 0, DateTimeKind.Utc)
        },
        new ContactInfo
        {
          ContactId = "6B017931-E247-48C7-8257-25B0ED239872",
          ContactType = "Application",
          Name = "James Liu",
          Email = "james.liu@detc.edu",
          HomePhoneNumber = null,
          MobilePhoneNumber = null,
          WorkPhoneNumber = "+1-555-338-3863",
          WorkPhoneExtension = null,
          Title = "Director of Development",
          Role = "Additional Signing Authority",
          IsPrimary = false,
          IsEditable = false,
          ApplicationId = Guid.Parse("3a1eac9f-da13-a888-883f-d2c0575e7620"),
          LastUpdated = new DateTime(2024, 1, 1, 12, 30, 0, DateTimeKind.Utc)
        }
      },
      _ => Array.Empty<ContactInfo>()
    };
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
      !storedContacts.Any(sc => string.Equals(sc.ContactId, dc.ContactId, StringComparison.OrdinalIgnoreCase)) &&
      !deletedDefaultIds.Contains(dc.ContactId)).ToArray();

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
          if (allContacts[i].ContactId == storedPrimary.ContactId)
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
      DataType = "CONTACTINFO",
      Contacts = allContacts
        .OrderByDescending(c => c.LastUpdated)
        .Select(c => new
        {
          c.ContactId,
          c.Name,
          c.Title,
          c.Email,
          c.HomePhoneNumber,
          c.MobilePhoneNumber,
          c.WorkPhoneNumber,
          c.WorkPhoneExtension,
          c.ContactType,
          c.Role,
          c.IsPrimary,
          c.IsEditable,
          c.ApplicationId
        }).ToArray()
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
      !storedContacts.Any(sc => string.Equals(sc.ContactId, dc.ContactId, StringComparison.OrdinalIgnoreCase)) &&
      !deletedDefaultIds.Contains(dc.ContactId)).ToArray();

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
          if (allContacts[i].ContactId == storedPrimary.ContactId)
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
      DataType = "CONTACTINFO",
      Contacts = allContacts
        .OrderByDescending(c => c.LastUpdated)
        .Select(c => new
        {
          c.ContactId,
          c.Name,
          c.Title,
          c.Email,
          c.HomePhoneNumber,
          c.MobilePhoneNumber,
          c.WorkPhoneNumber,
          c.WorkPhoneExtension,
          c.ContactType,
          c.Role,
          c.IsPrimary,
          c.IsEditable,
          c.ApplicationId
        }).ToArray()
    };
  }
}
