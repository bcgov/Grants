using Ardalis.Result;
using Grants.ApplicantPortal.API.Core.DTOs;

namespace Grants.ApplicantPortal.API.Core.Services;

/// <summary>
/// Service interface for plugin-based organization management operations
/// </summary>
public interface IOrganizationManagementService
{
  /// <summary>
  /// Edits an existing organization using the specified plugin
  /// </summary>
  /// <param name="editRequest">Organization edit request data</param>
  /// <param name="profileContext">Profile context information</param>
  /// <param name="cancellationToken">Cancellation token</param>
  /// <returns>Result indicating success or failure</returns>
  Task<Result> EditOrganizationAsync(
    EditOrganizationRequest editRequest,
    ProfileContext profileContext,
    CancellationToken cancellationToken = default);
}
