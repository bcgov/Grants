using Grants.ApplicantPortal.API.Infrastructure.Messaging.Abstractions;

namespace Grants.ApplicantPortal.API.Infrastructure.Messaging.Messages;

/// <summary>
/// Message sent when a profile is updated by a plugin
/// </summary>
public record ProfileUpdatedMessage : BaseMessage
{
    public ProfileUpdatedMessage(
        Guid profileId, 
        string pluginId, 
        string provider, 
        string key,
        string? correlationId = null) : base(correlationId, pluginId)
    {
        ProfileId = profileId;
        Provider = provider;
        Key = key;
    }

    /// <summary>
    /// ID of the profile that was updated
    /// </summary>
    public Guid ProfileId { get; }

    /// <summary>
    /// Provider that was updated (e.g., "PROGRAM1")
    /// </summary>
    public string Provider { get; }

    /// <summary>
    /// Key that was updated (e.g., "ORGINFO")
    /// </summary>
    public string Key { get; }
}

/// <summary>
/// Message sent when a contact is created
/// </summary>
public record ContactCreatedMessage : BaseMessage
{
    public ContactCreatedMessage(
        Guid contactId,
        Guid profileId,
        string pluginId,
        string contactName,
        string contactType,
        string? correlationId = null) : base(correlationId, pluginId)
    {
        ContactId = contactId;
        ProfileId = profileId;
        ContactName = contactName;
        ContactType = contactType;
    }

    /// <summary>
    /// ID of the created contact
    /// </summary>
    public Guid ContactId { get; }

    /// <summary>
    /// ID of the profile the contact belongs to
    /// </summary>
    public Guid ProfileId { get; }

    /// <summary>
    /// Name of the contact
    /// </summary>
    public string ContactName { get; }

    /// <summary>
    /// Type of contact (e.g., "PRIMARY", "GRANTS")
    /// </summary>
    public string ContactType { get; }
}

/// <summary>
/// Message sent when an address is updated
/// </summary>
public record AddressUpdatedMessage : BaseMessage
{
    public AddressUpdatedMessage(
        Guid addressId,
        Guid profileId,
        string pluginId,
        string addressType,
        bool isPrimary,
        string? correlationId = null) : base(correlationId, pluginId)
    {
        AddressId = addressId;
        ProfileId = profileId;
        AddressType = addressType;
        IsPrimary = isPrimary;
    }

    /// <summary>
    /// ID of the updated address
    /// </summary>
    public Guid AddressId { get; }

    /// <summary>
    /// ID of the profile the address belongs to
    /// </summary>
    public Guid ProfileId { get; }

    /// <summary>
    /// Type of address (e.g., "MAILING", "PHYSICAL")
    /// </summary>
    public string AddressType { get; }

    /// <summary>
    /// Whether this is the primary address
    /// </summary>
    public bool IsPrimary { get; }
}

/// <summary>
/// Message sent when an organization is updated
/// </summary>
public record OrganizationUpdatedMessage : BaseMessage
{
    public OrganizationUpdatedMessage(
        Guid organizationId,
        Guid profileId,
        string pluginId,
        string organizationName,
        string? organizationType = null,
        string? correlationId = null) : base(correlationId, pluginId)
    {
        OrganizationId = organizationId;
        ProfileId = profileId;
        OrganizationName = organizationName;
        OrganizationType = organizationType;
    }

    /// <summary>
    /// ID of the updated organization
    /// </summary>
    public Guid OrganizationId { get; }

    /// <summary>
    /// ID of the profile the organization belongs to
    /// </summary>
    public Guid ProfileId { get; }

    /// <summary>
    /// Name of the organization
    /// </summary>
    public string OrganizationName { get; }

    /// <summary>
    /// Type of organization
    /// </summary>
    public string? OrganizationType { get; }
}

/// <summary>
/// Message sent for system-level events
/// </summary>
public record SystemEventMessage : BaseMessage
{
    public SystemEventMessage(
        string eventType,
        string eventDescription,
        object? eventData = null,
        string? correlationId = null) : base(correlationId, "SYSTEM")
    {
        EventType = eventType;
        EventDescription = eventDescription;
        EventData = eventData;
    }

    /// <summary>
    /// Type of system event (e.g., "CLEANUP_COMPLETED", "JOB_FAILED")
    /// </summary>
    public string EventType { get; }

    /// <summary>
    /// Human-readable description of the event
    /// </summary>
    public string EventDescription { get; }

    /// <summary>
    /// Additional data related to the event
    /// </summary>
    public object? EventData { get; }
}

/// <summary>
/// Acknowledgment message sent by external systems in response to messages we sent
/// </summary>
public record MessageAcknowledgment : BaseMessage
{
    public MessageAcknowledgment(
        Guid originalMessageId,
        string status,
        string pluginId,
        string? details = null,
        string? correlationId = null) : base(correlationId, pluginId)
    {
        OriginalMessageId = originalMessageId;
        Status = status;
        Details = details;
        ProcessedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// ID of the original message this is acknowledging
    /// </summary>
    public Guid OriginalMessageId { get; }

    /// <summary>
    /// Status of the acknowledgment (SUCCESS, FAILED, PROCESSING)
    /// </summary>
    public string Status { get; }

    /// <summary>
    /// Additional details about the processing result
    /// </summary>
    public string? Details { get; }

    /// <summary>
    /// When the external system processed the message
    /// </summary>
    public DateTime ProcessedAt { get; }
}

/// <summary>
/// Generic plugin data message for external systems to send data updates
/// </summary>
public record PluginDataMessage : BaseMessage
{
    public PluginDataMessage(
        string pluginId,
        string dataType,
        object data,
        string? correlationId = null) : base(correlationId, pluginId)
    {
        DataType = dataType;
        Data = data;
    }

    /// <summary>
    /// Type of data being sent (e.g., "PROFILE_UPDATE", "CONTACT_SYNC")
    /// </summary>
    public string DataType { get; }

    /// <summary>
    /// The actual data payload
    /// </summary>
    public object Data { get; }
}
