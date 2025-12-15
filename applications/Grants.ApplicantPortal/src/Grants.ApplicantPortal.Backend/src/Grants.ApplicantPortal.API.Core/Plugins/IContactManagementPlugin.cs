using Ardalis.Result;
using Grants.ApplicantPortal.API.Core.DTOs;

namespace Grants.ApplicantPortal.API.Core.Plugins;

/// <summary>
/// Interface that plugins must implement to support contact management operations
/// </summary>
public interface IContactManagementPlugin
{
  /// <summary>
  /// Creates a new contact in the external system
  /// </summary>
  /// <param name="contactRequest">Contact creation request data</param>
  /// <param name="profileContext">Profile context information</param>
  /// <param name="cancellationToken">Cancellation token</param>
  /// <returns>Result containing the new contact ID</returns>
  Task<Result<Guid>> CreateContactAsync(
    CreateContactRequest contactRequest,
    ProfileContext profileContext,
    CancellationToken cancellationToken = default);

  /// <summary>
  /// Edits an existing contact in the external system
  /// </summary>
  /// <param name="editRequest">Contact edit request data</param>
  /// <param name="profileContext">Profile context information</param>
  /// <param name="cancellationToken">Cancellation token</param>
  /// <returns>Result indicating success or failure</returns>
  Task<Result> EditContactAsync(
    EditContactRequest editRequest,
    ProfileContext profileContext,
    CancellationToken cancellationToken = default);

  /// <summary>
  /// Sets a contact as the primary contact in the external system
  /// </summary>
  /// <param name="contactId">Contact ID to set as primary</param>
  /// <param name="profileContext">Profile context information</param>
  /// <param name="cancellationToken">Cancellation token</param>
  /// <returns>Result indicating success or failure</returns>
  Task<Result> SetAsPrimaryContactAsync(
    Guid contactId,
    ProfileContext profileContext,
    CancellationToken cancellationToken = default);

  /// <summary>
  /// Deletes a contact from the external system
  /// </summary>
  /// <param name="contactId">Contact ID to delete</param>
  /// <param name="profileContext">Profile context information</param>
  /// <param name="cancellationToken">Cancellation token</param>
  /// <returns>Result indicating success or failure</returns>
  Task<Result> DeleteContactAsync(
    Guid contactId,
    ProfileContext profileContext,
    CancellationToken cancellationToken = default);
}
