using Ardalis.Result;
using Grants.ApplicantPortal.API.Core.DTOs;
using Grants.ApplicantPortal.API.Core.Plugins;
using Grants.ApplicantPortal.API.Plugins.Demo.Data;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

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
        logger.LogInformation("Demo plugin creating contact for ProfileId: {ProfileId}, Name: {Name}, Type: {Type}",
            profileContext.ProfileId, contactRequest.Name, contactRequest.ContactType);

        try
        {
            // Simulate some processing time
            await Task.Delay(100, cancellationToken);

            // Add the contact to our in-memory store
            var newContactId = ContactsData.AddContact(profileContext.Provider, profileContext.ProfileId, contactRequest);

            // PERSIST TO REDIS: Update the cached contacts data
            await PersistContactsDataToRedis(profileContext.Provider, profileContext.ProfileId, cancellationToken);

            // Log the contact creation details
            logger.LogInformation("Demo plugin created contact - ID: {ContactId}, Name: {Name}, Type: {Type}, Email: {Email}",
                newContactId, contactRequest.Name, contactRequest.ContactType, contactRequest.Email);            

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

            // PERSIST TO REDIS: Update the cached contacts data
            await PersistContactsDataToRedis(profileContext.Provider, profileContext.ProfileId, cancellationToken);

            // Log the contact edit details
            logger.LogInformation("Demo plugin edited contact - ID: {ContactId}, Name: {Name}, Type: {Type}, Email: {Email}",
                editRequest.ContactId, editRequest.Name, editRequest.ContactType, editRequest.Email);

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

            // PERSIST TO REDIS: Update the cached contacts data
            await PersistContactsDataToRedis(profileContext.Provider, profileContext.ProfileId, cancellationToken);

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

            // PERSIST TO REDIS: Update the cached contacts data
            await PersistContactsDataToRedis(profileContext.Provider, profileContext.ProfileId, cancellationToken);

            // TRACK DELETION: Mark this contact as deleted to prevent re-seeding
            await TrackContactDeletion(contactId, profileContext.Provider, profileContext.ProfileId, cancellationToken);

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

    /// <summary>
    /// Persists the current contacts data to Redis cache
    /// </summary>
    private async Task PersistContactsDataToRedis(string provider, Guid profileId, CancellationToken cancellationToken)
    {
        try
        {
            // Generate the current contacts data using the existing ContactsData logic
            var metadata = new ProfilePopulationMetadata(
                profileId,
                PluginId,
                provider,
                "CONTACTINFO");

            var mockData = GenerateSeedingMockData(metadata);
            var jsonData = JsonSerializer.Serialize(mockData, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            });

            var profileData = new ProfileData(
                profileId,
                PluginId,
                provider,
                "CONTACTINFO",
                jsonData);

            // Store updated data in Redis — must use _jsonOptions (camelCase) to match the read path in PopulateProfileAsync
            var cacheKey = $"{_cacheOptions.Value.CacheKeyPrefix}{profileId}:DEMO:{provider}:CONTACTINFO";
            var profileDataBytes = JsonSerializer.SerializeToUtf8Bytes(profileData, _jsonOptions);
            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(365)
            };

            await distributedCache.SetAsync(cacheKey, profileDataBytes, cacheOptions, cancellationToken);
            
            logger.LogDebug("Persisted contacts data to Redis for ProfileId: {ProfileId}, Provider: {Provider}", 
                profileId, provider);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to persist contacts data to Redis for ProfileId: {ProfileId}, Provider: {Provider}", 
                profileId, provider);
            throw; // This is critical - if we can't persist, the operation should fail
        }
    }

    /// <summary>
    /// Tracks a contact deletion in Redis to prevent re-seeding
    /// </summary>
    private async Task TrackContactDeletion(Guid contactId, string provider, Guid profileId, CancellationToken cancellationToken)
    {
        try
        {
            // Store specific contact deletion marker in Redis
            var contactDeletionKey = $"{_cacheOptions.Value.CacheKeyPrefix}DELETED_CONTACT:{profileId}:DEMO:{provider}:{contactId}";
            var contactDeletionData = new
            {
                ContactId = contactId,
                ProfileId = profileId,
                Provider = provider,
                DeletedAt = DateTime.UtcNow,
                DeletedBy = PluginId
            };

            var deletionBytes = JsonSerializer.SerializeToUtf8Bytes(contactDeletionData);
            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(365) // Keep deletion records for a year
            };

            await distributedCache.SetAsync(contactDeletionKey, deletionBytes, cacheOptions, cancellationToken);
            
            logger.LogDebug("Tracked contact deletion in Redis - ContactId: {ContactId}, ProfileId: {ProfileId}, Provider: {Provider}", 
                contactId, profileId, provider);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to track contact deletion for ContactId: {ContactId}, ProfileId: {ProfileId}", 
                contactId, profileId);
            // Don't throw - deletion tracking failure shouldn't break the main operation
        }
    }

}
