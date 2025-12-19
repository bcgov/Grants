using Ardalis.Result;
using Grants.ApplicantPortal.API.Core.DTOs;
using Grants.ApplicantPortal.API.Plugins.Demo.Data;

namespace Grants.ApplicantPortal.API.Plugins.Demo;

/// <summary>
/// Organization management implementation for Demo plugin
/// </summary>
public partial class DemoPlugin
{
    public async Task<Result> EditOrganizationAsync(
        EditOrganizationRequest editRequest,
        ProfileContext profileContext,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Demo plugin editing organization {OrganizationId} for ProfileId: {ProfileId}",
            editRequest.OrganizationId, profileContext.ProfileId);

        try
        {
            // Simulate some processing time
            await Task.Delay(85, cancellationToken);

            // Update the organization in our in-memory store
            var success = OrganizationsData.UpdateOrganization(profileContext.Provider, profileContext.ProfileId, editRequest.OrganizationId, editRequest);
            
            if (!success)
            {
                _logger.LogWarning("Organization {OrganizationId} not found for ProfileId: {ProfileId}",
                    editRequest.OrganizationId, profileContext.ProfileId);
                return Result.NotFound();
            }

            // Log the organization edit details
            _logger.LogInformation("Demo plugin edited organization - ID: {OrganizationId}, Name: {Name}, Type: {Type}, Status: {Status}",
                editRequest.OrganizationId, editRequest.Name, editRequest.OrganizationType, editRequest.Status);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Demo plugin failed to edit organization {OrganizationId} for ProfileId: {ProfileId}",
                editRequest.OrganizationId, profileContext.ProfileId);
            return Result.Error("Failed to edit organization in demo system");
        }
    }
}
