using Ardalis.Result;
using Grants.ApplicantPortal.API.Core.DTOs;

namespace Grants.ApplicantPortal.API.Core.Services;

/// <summary>
/// Service interface for plugin-based contact management operations
/// </summary>
public interface IContactManagementService
{
  /// <summary>
  /// Creates a new contact using the specified plugin
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
  /// Edits an existing contact using the specified plugin
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
  /// Sets a contact as the primary contact using the specified plugin
  /// </summary>
  /// <param name="contactId">Contact ID to set as primary</param>
  /// <param name="applicantId">Applicant ID</param>
  /// <param name="profileContext">Profile context information</param>
  /// <param name="cancellationToken">Cancellation token</param>
  /// <returns>Result indicating success or failure</returns>
  Task<Result> SetAsPrimaryContactAsync(
    Guid contactId,
    Guid applicantId,
    ProfileContext profileContext,
    CancellationToken cancellationToken = default);

  /// <summary>
  /// Deletes a contact using the specified plugin
  /// </summary>
  /// <param name="contactId">Contact ID to delete</param>
  /// <param name="applicantId">Applicant ID</param>
  /// <param name="profileContext">Profile context information</param>
  /// <param name="cancellationToken">Cancellation token</param>
  /// <returns>Result indicating success or failure</returns>
  Task<Result> DeleteContactAsync(
    Guid contactId,
    Guid applicantId,
    ProfileContext profileContext,
    CancellationToken cancellationToken = default);
}
