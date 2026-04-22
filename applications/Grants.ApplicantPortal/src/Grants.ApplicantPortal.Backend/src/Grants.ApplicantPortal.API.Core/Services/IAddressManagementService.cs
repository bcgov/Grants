using Ardalis.Result;
using Grants.ApplicantPortal.API.Core.DTOs;

namespace Grants.ApplicantPortal.API.Core.Services;

/// <summary>
/// Service interface for plugin-based address management operations
/// </summary>
public interface IAddressManagementService
{
  /// <summary>
  /// Creates a new address using the specified plugin
  /// </summary>
  /// <param name="addressRequest">Address creation request data</param>
  /// <param name="profileContext">Profile context information</param>
  /// <param name="cancellationToken">Cancellation token</param>
  /// <returns>Result containing the new address ID</returns>
  Task<Result<Guid>> CreateAddressAsync(
    CreateAddressRequest addressRequest,
    ProfileContext profileContext,
    CancellationToken cancellationToken = default);

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

  /// <summary>
  /// Deletes an address using the specified plugin
  /// </summary>
  /// <param name="addressId">Address ID to delete</param>
  /// <param name="applicantId">Applicant ID associated with the address</param>
  /// <param name="profileContext">Profile context information</param>
  /// <param name="cancellationToken">Cancellation token</param>
  /// <returns>Result indicating success or failure</returns>
  Task<Result> DeleteAddressAsync(
    Guid addressId,
    Guid applicantId,
    ProfileContext profileContext,
    CancellationToken cancellationToken = default);
}
