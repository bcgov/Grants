using Ardalis.Result;
using Grants.ApplicantPortal.API.Core.DTOs;

namespace Grants.ApplicantPortal.API.Core.Plugins;

/// <summary>
/// Interface that plugins must implement to support address management operations
/// </summary>
public interface IAddressManagementPlugin
{
  /// <summary>
  /// Edits an existing address in the external system
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
  /// Sets an address as the primary address in the external system
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
