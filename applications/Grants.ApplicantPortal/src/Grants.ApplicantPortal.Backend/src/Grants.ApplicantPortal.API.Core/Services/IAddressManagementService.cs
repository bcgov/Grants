using Ardalis.Result;
using Grants.ApplicantPortal.API.Core.DTOs;

namespace Grants.ApplicantPortal.API.Core.Services;

/// <summary>
/// Service interface for plugin-based address management operations
/// </summary>
public interface IAddressManagementService
{
  /// <summary>
  /// Edits an existing address using the specified plugin
  /// </summary>
  /// <param name="editRequest">Address edit request data</param>
  /// <param name="profileContext">Profile context information</param>
  /// <param name="cancellationToken">Cancellation token</param>
  /// <returns>Result indicating success or failure</returns>
  Task<Result> EditAddressAsync(
    EditAddressRequest editRequest,
    ProfileContext profileContext,
    CancellationToken cancellationToken = default);

  /// <summary>
  /// Sets an address as the primary address using the specified plugin
  /// </summary>
  /// <param name="addressId">Address ID to set as primary</param>
  /// <param name="profileContext">Profile context information</param>
  /// <param name="cancellationToken">Cancellation token</param>
  /// <returns>Result indicating success or failure</returns>
  Task<Result> SetAsPrimaryAddressAsync(
    Guid addressId,
    ProfileContext profileContext,
    CancellationToken cancellationToken = default);
}
