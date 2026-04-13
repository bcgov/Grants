using Ardalis.Result;
using Grants.ApplicantPortal.API.Core.DTOs;

namespace Grants.ApplicantPortal.API.Core.Services;

/// <summary>
/// Validates that the authenticated user owns the resources they are trying to access or modify.
/// Prevents IDOR (Insecure Direct Object Reference) attacks by cross-referencing
/// client-supplied IDs (applicantId, contactId, addressId) against the user's cached profile data.
/// </summary>
public interface IResourceOwnershipValidator
{
  /// <summary>
  /// Validates that a contact exists in the user's cached profile data and is editable.
  /// </summary>
  /// <param name="contactId">The contact ID from the request</param>
  /// <param name="profileContext">The authenticated user's profile context</param>
  /// <param name="cancellationToken">Cancellation token</param>
  /// <returns>Ownership validation result</returns>
  Task<OwnershipValidationResult> ValidateContactOwnershipAsync(
    Guid contactId,
    ProfileContext profileContext,
    CancellationToken cancellationToken = default);

  /// <summary>
  /// Validates that an address exists in the user's cached profile data and is editable.
  /// </summary>
  /// <param name="addressId">The address ID from the request</param>
  /// <param name="profileContext">The authenticated user's profile context</param>
  /// <param name="cancellationToken">Cancellation token</param>
  /// <returns>Ownership validation result</returns>
  Task<OwnershipValidationResult> ValidateAddressOwnershipAsync(
    Guid addressId,
    ProfileContext profileContext,
    CancellationToken cancellationToken = default);

  /// <summary>
  /// Validates that an organization exists in the user's cached profile data.
  /// </summary>
  /// <param name="organizationId">The organization ID from the request</param>
  /// <param name="profileContext">The authenticated user's profile context</param>
  /// <param name="cancellationToken">Cancellation token</param>
  /// <returns>Ownership validation result</returns>
  Task<OwnershipValidationResult> ValidateOrganizationOwnershipAsync(
    Guid organizationId,
    ProfileContext profileContext,
    CancellationToken cancellationToken = default);

  /// <summary>
  /// Validates that a given applicantId belongs to the authenticated user's cached profile data.
  /// Used for create operations where no resource ID exists yet but applicantId is required.
  /// </summary>
  /// <param name="applicantId">The applicant ID from the request</param>
  /// <param name="profileContext">The authenticated user's profile context</param>
  /// <param name="cancellationToken">Cancellation token</param>
  /// <returns>Ownership validation result</returns>
  Task<OwnershipValidationResult> ValidateApplicantOwnershipAsync(
    Guid applicantId,
    ProfileContext profileContext,
    CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of a resource ownership validation check
/// </summary>
public record OwnershipValidationResult
{
  /// <summary>
  /// Whether the resource belongs to the authenticated user
  /// </summary>
  public bool IsOwned { get; init; }

  /// <summary>
  /// Whether the resource is editable (e.g., not linked to a submission)
  /// </summary>
  public bool IsEditable { get; init; }

  /// <summary>
  /// Error message when validation fails
  /// </summary>
  public string? ErrorMessage { get; init; }

  public static OwnershipValidationResult Success(bool isEditable = true) =>
    new() { IsOwned = true, IsEditable = isEditable };

  public static OwnershipValidationResult NotOwned(string? errorMessage = null) =>
    new() { IsOwned = false, IsEditable = false, ErrorMessage = errorMessage ?? "Resource does not belong to the authenticated user" };

  public static OwnershipValidationResult NotEditable(string? errorMessage = null) =>
    new() { IsOwned = true, IsEditable = false, ErrorMessage = errorMessage ?? "Resource is not editable" };
}
