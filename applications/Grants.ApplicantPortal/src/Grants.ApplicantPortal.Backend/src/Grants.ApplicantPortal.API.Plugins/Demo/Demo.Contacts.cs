using Ardalis.Result;
using Grants.ApplicantPortal.API.Core.DTOs;
using Grants.ApplicantPortal.API.Infrastructure.Messaging.Messages;
using Grants.ApplicantPortal.API.Plugins.Demo.Data;

namespace Grants.ApplicantPortal.API.Plugins.Demo;

/// <summary>
/// Contact management implementation for Demo plugin
/// </summary>
public partial class DemoPlugin
{
    public async Task<Result<Guid>> CreateContactAsync(
        CreateContactRequest contactRequest, 
        ProfileContext profileContext, 
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Demo plugin creating contact for ProfileId: {ProfileId}, Name: {Name}, Type: {Type}",
            profileContext.ProfileId, contactRequest.Name, contactRequest.Type);

        try
        {
            // Simulate some processing time
            await Task.Delay(100, cancellationToken);

            // Add the contact to our in-memory store
            var newContactId = ContactsData.AddContact(profileContext.Provider, profileContext.ProfileId, contactRequest);

            // Log the contact creation details
            _logger.LogInformation("Demo plugin created contact - ID: {ContactId}, Name: {Name}, Type: {Type}, Email: {Email}, Phone: {Phone}",
                newContactId, contactRequest.Name, contactRequest.Type, contactRequest.Email, contactRequest.PhoneNumber);

            // Fire a message when contact is created
            var contactId = Guid.Parse(newContactId);
            await FireContactCreatedMessage(contactId, profileContext.ProfileId, contactRequest.Name, contactRequest.Type, cancellationToken);

            return Result<Guid>.Success(contactId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Demo plugin failed to create contact for ProfileId: {ProfileId}, Name: {Name}",
                profileContext.ProfileId, contactRequest.Name);
            return Result<Guid>.Error("Failed to create contact in demo system");
        }
    }

    public async Task<Result> EditContactAsync(
        EditContactRequest editRequest,
        ProfileContext profileContext,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Demo plugin editing contact {ContactId} for ProfileId: {ProfileId}",
            editRequest.ContactId, profileContext.ProfileId);

        try
        {
            // Simulate some processing time
            await Task.Delay(80, cancellationToken);

            // Update the contact in our in-memory store
            var success = ContactsData.UpdateContact(profileContext.Provider, profileContext.ProfileId, editRequest.ContactId, editRequest);
            
            if (!success)
            {
                _logger.LogWarning("Contact {ContactId} not found for ProfileId: {ProfileId}",
                    editRequest.ContactId, profileContext.ProfileId);
                return Result.NotFound();
            }

            // Log the contact edit details
            _logger.LogInformation("Demo plugin edited contact - ID: {ContactId}, Name: {Name}, Type: {Type}, Email: {Email}, Phone: {Phone}",
                editRequest.ContactId, editRequest.Name, editRequest.Type, editRequest.Email, editRequest.PhoneNumber);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Demo plugin failed to edit contact {ContactId} for ProfileId: {ProfileId}",
                editRequest.ContactId, profileContext.ProfileId);
            return Result.Error("Failed to edit contact in demo system");
        }
    }

    public async Task<Result> SetAsPrimaryContactAsync(
        Guid contactId,
        ProfileContext profileContext,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Demo plugin setting contact {ContactId} as primary for ProfileId: {ProfileId}",
            contactId, profileContext.ProfileId);

        try
        {
            // Simulate some processing time
            await Task.Delay(60, cancellationToken);

            // Set the contact as primary in our in-memory store
            var success = ContactsData.SetContactAsPrimary(profileContext.Provider, profileContext.ProfileId, contactId);
            
            if (!success)
            {
                _logger.LogWarning("Contact {ContactId} not found for ProfileId: {ProfileId}",
                    contactId, profileContext.ProfileId);
                return Result.NotFound();
            }

            // Log the contact set as primary operation
            _logger.LogInformation("Demo plugin set contact {ContactId} as primary for ProfileId: {ProfileId}",
                contactId, profileContext.ProfileId);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Demo plugin failed to set contact {ContactId} as primary for ProfileId: {ProfileId}",
                contactId, profileContext.ProfileId);
            return Result.Error("Failed to set contact as primary in demo system");
        }
    }

    public async Task<Result> DeleteContactAsync(
        Guid contactId,
        ProfileContext profileContext,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Demo plugin deleting contact {ContactId} for ProfileId: {ProfileId}",
            contactId, profileContext.ProfileId);

        try
        {
            // Simulate some processing time
            await Task.Delay(90, cancellationToken);

            // Delete the contact from our in-memory store
            var success = ContactsData.DeleteContact(profileContext.Provider, profileContext.ProfileId, contactId);
            
            if (!success)
            {
                _logger.LogWarning("Contact {ContactId} not found for ProfileId: {ProfileId}",
                    contactId, profileContext.ProfileId);
                return Result.NotFound();
            }

            // Log the contact deletion
            _logger.LogInformation("Demo plugin deleted contact {ContactId} for ProfileId: {ProfileId}",
                contactId, profileContext.ProfileId);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Demo plugin failed to delete contact {ContactId} for ProfileId: {ProfileId}",
                contactId, profileContext.ProfileId);
            return Result.Error("Failed to delete contact in demo system");
        }
    }

    /// <summary>
    /// Helper method to fire contact created message
    /// </summary>
    private async Task FireContactCreatedMessage(Guid contactId, Guid profileId, string contactName, string contactType, CancellationToken cancellationToken)
    {
        if (_messagePublisher == null)
        {
            _logger.LogDebug("Message publisher not available - skipping ContactCreatedMessage");
            return;
        }

        try
        {
            var message = new ContactCreatedMessage(
                contactId,
                profileId,
                PluginId,
                contactName,
                contactType,
                correlationId: $"profile-{profileId}");

            await _messagePublisher.PublishAsync(message, cancellationToken);
            
            _logger.LogDebug("Published ContactCreatedMessage for contact {ContactId} in profile {ProfileId}", 
                contactId, profileId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to publish ContactCreatedMessage for contact {ContactId}", contactId);
            // Don't throw - messaging failures shouldn't break the main operation
        }
    }
}
