using System.Text.Json;
using Ardalis.Result;
using Grants.ApplicantPortal.API.Core.DTOs;
using Grants.ApplicantPortal.API.Core.Plugins;

namespace Grants.ApplicantPortal.API.Plugins.Demo;

/// <summary>
/// Demo profile plugin for testing and demonstration purposes
/// </summary>
public class DemoProfilePlugin(ILogger<DemoProfilePlugin> logger) : IProfilePlugin, IContactManagementPlugin, IAddressManagementPlugin, IOrganizationManagementPlugin
{
  public string PluginId => "DEMO";

  public IReadOnlyList<PluginSupportedFeature> GetSupportedFeatures()
  {
    return DemoPluginFeatures.SupportedFeatures;
  }

  public IReadOnlyList<string> GetSupportedProviders()
  {
    return DemoPluginFeatures.GetSupportedProviders();
  }

  public IReadOnlyList<string> GetSupportedKeys(string provider)
  {
    return DemoPluginFeatures.GetSupportedKeys(provider);
  }

  public bool CanHandle(ProfilePopulationMetadata metadata)
  {
    if (!metadata.PluginId.Equals(PluginId, StringComparison.OrdinalIgnoreCase))
      return false;

    // Use the centralized feature validation
    return DemoPluginFeatures.IsProviderKeySupported(metadata.Provider, metadata.Key);
  }

  public async Task<ProfileData> PopulateProfileAsync(ProfilePopulationMetadata metadata, CancellationToken cancellationToken = default)
  {
    logger.LogInformation("Demo plugin populating profile for ProfileId: {ProfileId}, Provider: {Provider}, Key: {Key}",
        metadata.ProfileId, metadata.Provider, metadata.Key);

    try
    {
      // Simulate some processing time
      await Task.Delay(50, cancellationToken);

      var mockProfileData = GenerateMockData(metadata);

      var jsonData = JsonSerializer.Serialize(mockProfileData, new JsonSerializerOptions
      {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
      });

      logger.LogInformation("Demo plugin successfully populated profile for ProfileId: {ProfileId}, Provider: {Provider}, Key: {Key}",
          metadata.ProfileId, metadata.Provider, metadata.Key);

      return new ProfileData(
          metadata.ProfileId,
          metadata.PluginId,
          metadata.Provider,
          metadata.Key,
          jsonData);
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Demo plugin failed to populate profile for ProfileId: {ProfileId}, Provider: {Provider}, Key: {Key}",
          metadata.ProfileId, metadata.Provider, metadata.Key);
      throw;
    }
  }

  private object GenerateMockData(ProfilePopulationMetadata metadata)
  {
    var baseData = new
    {
      ProfileId = metadata.ProfileId,
      Provider = metadata.Provider,
      Key = metadata.Key,
      Source = "Demo System",
      PopulatedAt = DateTime.UtcNow,
      PopulatedBy = PluginId
    };

    return (metadata.Provider?.ToUpper(), metadata.Key?.ToUpper()) switch
    {
      // Use dedicated data classes for each type
      ("PROGRAM1", "SUBMISSIONS") => SubmissionsData.GenerateProgram1Submissions(baseData),
      ("PROGRAM1", "ORGINFO") => OrganizationsData.GenerateProgram1OrgInfo(baseData),
      ("PROGRAM1", "PAYMENTS") => OrganizationsData.GenerateProgram1Payments(baseData),
      ("PROGRAM1", "CONTACTS") => ContactsData.GenerateProgram1Contacts(baseData),
      ("PROGRAM1", "ADDRESSES") => AddressesData.GenerateProgram1Addresses(baseData),
      ("PROGRAM2", "SUBMISSIONS") => SubmissionsData.GenerateProgram2Submissions(baseData),
      ("PROGRAM2", "ORGINFO") => OrganizationsData.GenerateProgram2OrgInfo(baseData),
      ("PROGRAM2", "CONTACTS") => ContactsData.GenerateProgram2Contacts(baseData),
      ("PROGRAM2", "ADDRESSES") => AddressesData.GenerateProgram2Addresses(baseData),
      _ => OrganizationsData.GenerateDefaultData(baseData)
    };
  }

  public async Task<Result<Guid>> CreateContactAsync(
    CreateContactRequest contactRequest, 
    ProfileContext profileContext, 
    CancellationToken cancellationToken = default)
  {
    logger.LogInformation("Demo plugin creating contact for ProfileId: {ProfileId}, Name: {Name}, Type: {Type}",
      profileContext.ProfileId, contactRequest.Name, contactRequest.Type);

    try
    {
      // Simulate some processing time
      await Task.Delay(100, cancellationToken);

      // Add the contact to our in-memory store
      var newContactId = ContactsData.AddContact(profileContext.Provider, profileContext.ProfileId, contactRequest);

      // Log the contact creation details
      logger.LogInformation("Demo plugin created contact - ID: {ContactId}, Name: {Name}, Type: {Type}, Email: {Email}, Phone: {Phone}",
        newContactId, contactRequest.Name, contactRequest.Type, contactRequest.Email, contactRequest.PhoneNumber);

      return Result<Guid>.Success(Guid.Parse(newContactId));
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Demo plugin failed to create contact for ProfileId: {ProfileId}, Name: {Name}",
        profileContext.ProfileId, contactRequest.Name);
      return Result<Guid>.Error("Failed to create contact in demo system");
    }
  }

  public async Task<Result> EditContactAsync(
    EditContactRequest editRequest,
    ProfileContext profileContext,
    CancellationToken cancellationToken = default)
  {
    logger.LogInformation("Demo plugin editing contact {ContactId} for ProfileId: {ProfileId}",
      editRequest.ContactId, profileContext.ProfileId);

    try
    {
      // Simulate some processing time
      await Task.Delay(80, cancellationToken);

      // Update the contact in our in-memory store
      var success = ContactsData.UpdateContact(profileContext.Provider, profileContext.ProfileId, editRequest.ContactId, editRequest);
      
      if (!success)
      {
        logger.LogWarning("Contact {ContactId} not found for ProfileId: {ProfileId}",
          editRequest.ContactId, profileContext.ProfileId);
        return Result.NotFound();
      }

      // Log the contact edit details
      logger.LogInformation("Demo plugin edited contact - ID: {ContactId}, Name: {Name}, Type: {Type}, Email: {Email}, Phone: {Phone}",
        editRequest.ContactId, editRequest.Name, editRequest.Type, editRequest.Email, editRequest.PhoneNumber);

      return Result.Success();
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Demo plugin failed to edit contact {ContactId} for ProfileId: {ProfileId}",
        editRequest.ContactId, profileContext.ProfileId);
      return Result.Error("Failed to edit contact in demo system");
    }
  }

  public async Task<Result> SetAsPrimaryContactAsync(
    Guid contactId,
    ProfileContext profileContext,
    CancellationToken cancellationToken = default)
  {
    logger.LogInformation("Demo plugin setting contact {ContactId} as primary for ProfileId: {ProfileId}",
      contactId, profileContext.ProfileId);

    try
    {
      // Simulate some processing time
      await Task.Delay(60, cancellationToken);

      // Set the contact as primary in our in-memory store
      var success = ContactsData.SetContactAsPrimary(profileContext.Provider, profileContext.ProfileId, contactId);
      
      if (!success)
      {
        logger.LogWarning("Contact {ContactId} not found for ProfileId: {ProfileId}",
          contactId, profileContext.ProfileId);
        return Result.NotFound();
      }

      // Log the contact set as primary operation
      logger.LogInformation("Demo plugin set contact {ContactId} as primary for ProfileId: {ProfileId}",
        contactId, profileContext.ProfileId);

      return Result.Success();
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Demo plugin failed to set contact {ContactId} as primary for ProfileId: {ProfileId}",
        contactId, profileContext.ProfileId);
      return Result.Error("Failed to set contact as primary in demo system");
    }
  }

  public async Task<Result> DeleteContactAsync(
    Guid contactId,
    ProfileContext profileContext,
    CancellationToken cancellationToken = default)
  {
    logger.LogInformation("Demo plugin deleting contact {ContactId} for ProfileId: {ProfileId}",
      contactId, profileContext.ProfileId);

    try
    {
      // Simulate some processing time
      await Task.Delay(90, cancellationToken);

      // Delete the contact from our in-memory store
      var success = ContactsData.DeleteContact(profileContext.Provider, profileContext.ProfileId, contactId);
      
      if (!success)
      {
        logger.LogWarning("Contact {ContactId} not found for ProfileId: {ProfileId}",
          contactId, profileContext.ProfileId);
        return Result.NotFound();
      }

      // Log the contact deletion
      logger.LogInformation("Demo plugin deleted contact {ContactId} for ProfileId: {ProfileId}",
        contactId, profileContext.ProfileId);

      return Result.Success();
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Demo plugin failed to delete contact {ContactId} for ProfileId: {ProfileId}",
        contactId, profileContext.ProfileId);
      return Result.Error("Failed to delete contact in demo system");
    }
  }

  public async Task<Result> EditAddressAsync(
    EditAddressRequest editRequest,
    ProfileContext profileContext,
    CancellationToken cancellationToken = default)
  {
    logger.LogInformation("Demo plugin editing address {AddressId} for ProfileId: {ProfileId}",
      editRequest.AddressId, profileContext.ProfileId);

    try
    {
      // Simulate some processing time
      await Task.Delay(75, cancellationToken);

      // Update the address in our in-memory store
      var success = AddressesData.UpdateAddress(profileContext.Provider, profileContext.ProfileId, editRequest.AddressId, editRequest);
      
      if (!success)
      {
        logger.LogWarning("Address {AddressId} not found for ProfileId: {ProfileId}",
          editRequest.AddressId, profileContext.ProfileId);
        return Result.NotFound();
      }

      // Log the address edit details
      logger.LogInformation("Demo plugin edited address - ID: {AddressId}, Type: {Type}, Address: {Address}, City: {City}",
        editRequest.AddressId, editRequest.Type, editRequest.Address, editRequest.City);

      return Result.Success();
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Demo plugin failed to edit address {AddressId} for ProfileId: {ProfileId}",
        editRequest.AddressId, profileContext.ProfileId);
      return Result.Error("Failed to edit address in demo system");
    }
  }

  public async Task<Result> SetAsPrimaryAddressAsync(
    Guid addressId,
    ProfileContext profileContext,
    CancellationToken cancellationToken = default)
  {
    logger.LogInformation("Demo plugin setting address {AddressId} as primary for ProfileId: {ProfileId}",
      addressId, profileContext.ProfileId);

    try
    {
      // Simulate some processing time
      await Task.Delay(55, cancellationToken);

      // Set the address as primary in our in-memory store
      var success = AddressesData.SetAddressAsPrimary(profileContext.Provider, profileContext.ProfileId, addressId);
      
      if (!success)
      {
        logger.LogWarning("Address {AddressId} not found for ProfileId: {ProfileId}",
          addressId, profileContext.ProfileId);
        return Result.NotFound();
      }

      // Log the address set as primary operation
      logger.LogInformation("Demo plugin set address {AddressId} as primary for ProfileId: {ProfileId}",
        addressId, profileContext.ProfileId);

      return Result.Success();
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Demo plugin failed to set address {AddressId} as primary for ProfileId: {ProfileId}",
        addressId, profileContext.ProfileId);
      return Result.Error("Failed to set address as primary in demo system");
    }
  }

  public async Task<Result> EditOrganizationAsync(
    EditOrganizationRequest editRequest,
    ProfileContext profileContext,
    CancellationToken cancellationToken = default)
  {
    logger.LogInformation("Demo plugin editing organization {OrganizationId} for ProfileId: {ProfileId}",
      editRequest.OrganizationId, profileContext.ProfileId);

    try
    {
      // Simulate some processing time
      await Task.Delay(85, cancellationToken);

      // Update the organization in our in-memory store
      var success = OrganizationsData.UpdateOrganization(profileContext.Provider, profileContext.ProfileId, editRequest.OrganizationId, editRequest);
      
      if (!success)
      {
        logger.LogWarning("Organization {OrganizationId} not found for ProfileId: {ProfileId}",
          editRequest.OrganizationId, profileContext.ProfileId);
        return Result.NotFound();
      }

      // Log the organization edit details
      logger.LogInformation("Demo plugin edited organization - ID: {OrganizationId}, Name: {Name}, Type: {Type}, Status: {Status}",
        editRequest.OrganizationId, editRequest.Name, editRequest.OrganizationType, editRequest.Status);

      return Result.Success();
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Demo plugin failed to edit organization {OrganizationId} for ProfileId: {ProfileId}",
        editRequest.OrganizationId, profileContext.ProfileId);
      return Result.Error("Failed to edit organization in demo system");
    }
  }
}
