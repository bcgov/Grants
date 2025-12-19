using Ardalis.Result;
using Grants.ApplicantPortal.API.Core.DTOs;
using Grants.ApplicantPortal.API.Plugins.Demo.Data;

namespace Grants.ApplicantPortal.API.Plugins.Demo;

/// <summary>
/// Address management implementation for Demo plugin
/// </summary>
public partial class DemoPlugin
{
    public async Task<Result> EditAddressAsync(
        EditAddressRequest editRequest,
        ProfileContext profileContext,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Demo plugin editing address {AddressId} for ProfileId: {ProfileId}",
            editRequest.AddressId, profileContext.ProfileId);

        try
        {
            // Simulate some processing time
            await Task.Delay(75, cancellationToken);

            // Update the address in our in-memory store
            var success = AddressesData.UpdateAddress(profileContext.Provider, profileContext.ProfileId, editRequest.AddressId, editRequest);
            
            if (!success)
            {
                _logger.LogWarning("Address {AddressId} not found for ProfileId: {ProfileId}",
                    editRequest.AddressId, profileContext.ProfileId);
                return Result.NotFound();
            }

            // Log the address edit details
            _logger.LogInformation("Demo plugin edited address - ID: {AddressId}, Type: {Type}, Address: {Address}, City: {City}",
                editRequest.AddressId, editRequest.Type, editRequest.Address, editRequest.City);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Demo plugin failed to edit address {AddressId} for ProfileId: {ProfileId}",
                editRequest.AddressId, profileContext.ProfileId);
            return Result.Error("Failed to edit address in demo system");
        }
    }

    public async Task<Result> SetAsPrimaryAddressAsync(
        Guid addressId,
        ProfileContext profileContext,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Demo plugin setting address {AddressId} as primary for ProfileId: {ProfileId}",
            addressId, profileContext.ProfileId);

        try
        {
            // Simulate some processing time
            await Task.Delay(55, cancellationToken);

            // Set the address as primary in our in-memory store
            var success = AddressesData.SetAddressAsPrimary(profileContext.Provider, profileContext.ProfileId, addressId);
            
            if (!success)
            {
                _logger.LogWarning("Address {AddressId} not found for ProfileId: {ProfileId}",
                    addressId, profileContext.ProfileId);
                return Result.NotFound();
            }

            // Log the address set as primary operation
            _logger.LogInformation("Demo plugin set address {AddressId} as primary for ProfileId: {ProfileId}",
                addressId, profileContext.ProfileId);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Demo plugin failed to set address {AddressId} as primary for ProfileId: {ProfileId}",
                addressId, profileContext.ProfileId);
            return Result.Error("Failed to set address as primary in demo system");
        }
    }
}
