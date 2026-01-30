using Grants.ApplicantPortal.API.Core.Entities;

namespace Grants.ApplicantPortal.API.Core.Features.Security.SecurityLogAggregate;

/// <summary>
/// Security audit log entity for tracking security events
/// </summary>
public class SecurityLog : FullAuditedEntity<Guid>, IAggregateRoot
{
    /// <summary>
    /// The profile ID associated with this security event
    /// </summary>
    public required Guid ProfileId { get; set; }

    /// <summary>
    /// Type of security event (Login, Logout, etc.)
    /// </summary>
    public required string EventType { get; set; }

    /// <summary>
    /// Detailed description of the security event
    /// </summary>
    public string? EventDescription { get; set; }

    /// <summary>
    /// User's IP address when the event occurred
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// User agent string from the request
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// Session identifier if available
    /// </summary>
    public string? SessionId { get; set; }

    /// <summary>
    /// Additional metadata about the event in JSON format
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// Whether this event is considered successful
    /// </summary>
    public bool IsSuccessful { get; set; }

    /// <summary>
    /// Error message if the event was not successful
    /// </summary>
    public string? ErrorMessage { get; set; }

    public SecurityLog()
    {
        // Required properties will be set during object initialization
    }

    public SecurityLog SetProfileId(Guid profileId)
    {
        ProfileId = Guard.Against.Default(profileId);
        return this;
    }

    public SecurityLog SetEventType(string eventType)
    {
        EventType = Guard.Against.NullOrEmpty(eventType);
        return this;
    }

    public SecurityLog SetEventDescription(string? eventDescription)
    {
        EventDescription = eventDescription;
        return this;
    }

    public SecurityLog SetIpAddress(string? ipAddress)
    {
        IpAddress = ipAddress;
        return this;
    }

    public SecurityLog SetUserAgent(string? userAgent)
    {
        UserAgent = userAgent;
        return this;
    }

    public SecurityLog SetSessionId(string? sessionId)
    {
        SessionId = sessionId;
        return this;
    }

    public SecurityLog SetMetadata(string? metadata)
    {
        Metadata = metadata;
        return this;
    }

    public SecurityLog SetIsSuccessful(bool isSuccessful)
    {
        IsSuccessful = isSuccessful;
        return this;
    }

    public SecurityLog SetErrorMessage(string? errorMessage)
    {
        ErrorMessage = errorMessage;
        return this;
    }
}
