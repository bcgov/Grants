using System.Security.Claims;
using Grants.ApplicantPortal.API.Core.Features.Profiles.ProfileAggregate;
using Grants.ApplicantPortal.API.Core.Features.Security.SecurityLogAggregate;

namespace Grants.ApplicantPortal.API.Web.Profiles;

/// <summary>
/// Service for managing user profiles and profile resolution
/// </summary>
public interface IProfileService
{
    /// <summary>
    /// Get or create a profile based on the user's claims (iss + sub)
    /// </summary>
    /// <param name="principal">The authenticated user's claims principal</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The profile associated with the user</returns>
    Task<Profile> GetOrCreateProfileAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get a profile by its unique identifier
    /// </summary>
    /// <param name="profileId">The profile's unique identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The profile if found, null otherwise</returns>
    Task<Profile?> GetProfileAsync(Guid profileId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get a profile by subject and issuer
    /// </summary>
    /// <param name="subject">The user's subject identifier</param>
    /// <param name="issuer">The user's issuer</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The profile if found, null otherwise</returns>
    Task<Profile?> GetProfileAsync(string subject, string issuer, CancellationToken cancellationToken = default);

    /// <summary>
    /// Log a security event
    /// </summary>
    /// <param name="profileId">The profile ID associated with the event</param>
    /// <param name="eventType">Type of security event</param>
    /// <param name="eventDescription">Description of the event</param>
    /// <param name="ipAddress">IP address of the user</param>
    /// <param name="userAgent">User agent string</param>
    /// <param name="sessionId">Session identifier</param>
    /// <param name="isSuccessful">Whether the event was successful</param>
    /// <param name="errorMessage">Error message if unsuccessful</param>
    /// <param name="metadata">Additional metadata in JSON format</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created security log entry</returns>
    Task<SecurityLog> LogSecurityEventAsync(
        Guid profileId,
        string eventType,
        string? eventDescription = null,
        string? ipAddress = null,
        string? userAgent = null,
        string? sessionId = null,
        bool isSuccessful = true,
        string? errorMessage = null,
        string? metadata = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get security logs for a profile
    /// </summary>
    /// <param name="profileId">The profile ID to get logs for</param>
    /// <param name="eventType">Optional event type filter</param>
    /// <param name="from">Optional start date filter</param>
    /// <param name="to">Optional end date filter</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of security logs</returns>
    Task<IEnumerable<SecurityLog>> GetSecurityLogsAsync(
        Guid profileId,
        string? eventType = null,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken cancellationToken = default);
}
