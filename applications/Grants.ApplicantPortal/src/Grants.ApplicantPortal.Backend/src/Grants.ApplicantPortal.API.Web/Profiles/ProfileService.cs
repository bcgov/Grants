using System.Security.Claims;
using System.Text.Json;
using Grants.ApplicantPortal.API.Core.Features.Profiles.ProfileAggregate;
using Grants.ApplicantPortal.API.Core.Features.Security.SecurityLogAggregate;
using Grants.ApplicantPortal.API.Infrastructure.Data;
using Grants.ApplicantPortal.API.Web.Auth;
using Microsoft.EntityFrameworkCore;

namespace Grants.ApplicantPortal.API.Web.Profiles;

/// <summary>
/// Service for managing user profiles and profile resolution
/// </summary>
public class ProfileService(
    AppDbContext context,
    ILogger<ProfileService> logger) : IProfileService
{
    private readonly AppDbContext _context = context;
    private readonly ILogger<ProfileService> _logger = logger;

    public async Task<Profile> GetOrCreateProfileAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default)
    {
        var subject = principal.GetSubject();
        var issuer = principal.FindFirst("iss")?.Value;

        if (string.IsNullOrEmpty(subject))
        {
            throw new InvalidOperationException("User subject claim is missing or empty");
        }

        if (string.IsNullOrEmpty(issuer))
        {
            throw new InvalidOperationException("User issuer claim is missing or empty");
        }

        _logger.LogDebug("Looking up profile for subject: {Subject}, issuer: {Issuer}", subject, issuer);

        // Try to find existing profile
        var existingProfile = await _context.Profiles
            .FirstOrDefaultAsync(p => p.Subject == subject && p.Issuer == issuer, cancellationToken);

        if (existingProfile != null)
        {
            _logger.LogDebug("Found existing profile: {ProfileId}", existingProfile.Id);
            return existingProfile;
        }

        // Create new profile
        _logger.LogInformation("Creating new profile for subject: {Subject}, issuer: {Issuer}", subject, issuer);

        var metadata = ExtractUserMetadata(principal);
        var profile = new Profile
        {
            Subject = subject,
            Issuer = issuer,
            IsActive = true
        }.SetMetadata(metadata);

        _context.Profiles.Add(profile);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created new profile: {ProfileId} for subject: {Subject}", profile.Id, subject);

        return profile;
    }

    public async Task<Profile?> GetProfileAsync(Guid profileId, CancellationToken cancellationToken = default)
    {
        return await _context.Profiles
            .FirstOrDefaultAsync(p => p.Id == profileId, cancellationToken);
    }

    public async Task<Profile?> GetProfileAsync(string subject, string issuer, CancellationToken cancellationToken = default)
    {
        return await _context.Profiles
            .FirstOrDefaultAsync(p => p.Subject == subject && p.Issuer == issuer, cancellationToken);
    }

    public async Task<SecurityLog> LogSecurityEventAsync(
        Guid profileId,
        string eventType,
        string? eventDescription = null,
        string? ipAddress = null,
        string? userAgent = null,
        string? sessionId = null,
        bool isSuccessful = true,
        string? errorMessage = null,
        string? metadata = null,
        CancellationToken cancellationToken = default)
    {
        var securityLog = new SecurityLog
        {
            ProfileId = profileId,
            EventType = eventType,
            EventDescription = eventDescription,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            SessionId = sessionId,
            IsSuccessful = isSuccessful,
            ErrorMessage = errorMessage,
            Metadata = metadata
        };

        _context.SecurityLogs.Add(securityLog);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Logged security event: {EventType} for profile: {ProfileId}, successful: {IsSuccessful}",
            eventType, profileId, isSuccessful);

        return securityLog;
    }

    public async Task<IEnumerable<SecurityLog>> GetSecurityLogsAsync(
        Guid profileId,
        string? eventType = null,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.SecurityLogs
            .Where(s => s.ProfileId == profileId);

        if (!string.IsNullOrEmpty(eventType))
        {
            query = query.Where(s => s.EventType == eventType);
        }

        if (from.HasValue)
        {
            query = query.Where(s => s.CreatedAt >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(s => s.CreatedAt <= to.Value);
        }

        return await query
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    private static string ExtractUserMetadata(ClaimsPrincipal principal)
    {
        var metadata = new Dictionary<string, object?>();

        // Extract useful claims
        var email = principal.GetEmail();
        var name = principal.GetFullName();
        var givenName = principal.GetGivenName();
        var familyName = principal.GetFamilyName();
        var username = principal.GetPreferredUsername();

        if (!string.IsNullOrEmpty(email))
            metadata["email"] = email;
        if (!string.IsNullOrEmpty(name))
            metadata["name"] = name;
        if (!string.IsNullOrEmpty(givenName))
            metadata["givenName"] = givenName;
        if (!string.IsNullOrEmpty(familyName))
            metadata["familyName"] = familyName;
        if (!string.IsNullOrEmpty(username))
            metadata["username"] = username;

        // Add roles
        var roles = principal.GetRealmRoles().ToList();
        if (roles.Count > 0)
            metadata["roles"] = roles;

        return JsonSerializer.Serialize(metadata);
    }
}
