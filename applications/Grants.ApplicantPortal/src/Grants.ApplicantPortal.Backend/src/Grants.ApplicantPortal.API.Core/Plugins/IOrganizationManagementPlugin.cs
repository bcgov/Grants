using Ardalis.Result;
using Grants.ApplicantPortal.API.Core.DTOs;

namespace Grants.ApplicantPortal.API.Core.Plugins;

/// <summary>
/// Interface that plugins must implement to support organization management operations
/// </summary>
public interface IOrganizationManagementPlugin
{
  /// <summary>
  /// Edits an existing organization in the external system
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
