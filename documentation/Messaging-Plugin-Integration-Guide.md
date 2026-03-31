# Messaging System - Plugin Integration Guide

## Overview

This messaging system provides **reliable, asynchronous communication** between the Grants Applicant Portal and external systems using the **Inbox/Outbox pattern**. The infrastructure is **plugin-agnostic** - any plugin can use it by publishing messages to the outbox and registering a handler for incoming responses.

### Key Concepts

- **Outbox**: Transactional store for outbound commands. The portal writes here; a background job publishes to RabbitMQ.
- **Inbox**: Transactional store for inbound messages. RabbitMQ delivers here; a background job routes to plugin handlers.
- **Plugin-agnostic routing**: The `PluginId` on every message drives routing. Add a new plugin, register a handler, done.

---

## Architecture

```
Portal                                      RabbitMQ                           External System
=======                                     ========                           ===============

Plugin fires command                        Exchange: grants.messaging
  via IMessagePublisher                     (topic)
        |                                       |
        v                                       |
  OutboxMessage table                           |
  (status: Pending)                             |
        |                                       |
  OutboxProcessorJob (Quartz)                   |
        |                                       |
        +--- publishes to RabbitMQ ------------>|
             routing key:                       |
                         commands.{plugin}.plugindata       +---> External consumer queue
             OutboxMessage (Published)                     |     (e.g. unity.commands
                   |                                       |      bound to commands.unity.plugindata)
        |                                       |
  OutboxTimeoutJob (Quartz)                     |     External processes command,
    If Published > AckTimeoutMinutes:           |     publishes ack back:
    - Records PluginEvent (AckTimeout)          |<--- routing key: grants.{plugin}.acknowledgment
    - Invalidates cache segment                 |
    - Marks outbox msg TimedOut                 |
                                                |
  RabbitMQConsumer (Portal)  <------------------+
  queue: grants.messaging.inbox
  bound to: grants.*.#
        |
        v
  InboxMessage table
  (status: Pending)
        |
  InboxProcessorJob (Quartz)
        |
        +--- Closes outbox loop:
        |    Marks original outbox msg Acknowledged
        |    If FAILED ack: records PluginEvent
        |      (InboxRejection) + invalidates cache
        |
  PluginMessageRouter
        |
        +---> UnityPluginMessageHandler   (PluginId = "UNITY")
        +---> DemoPluginMessageHandler    (PluginId = "DEMO")
        +---> YourNewPluginHandler        (PluginId = "NEWPLUGIN")
```

### Routing Key Namespaces

Two routing key prefixes keep outbound and inbound traffic separated:

| Direction | Prefix | Example | Consumed by |
|-----------|--------|---------|-------------|
| **Outbound** (Portal to External) | `commands.` | `commands.unity.plugindata` | External system queue |
| **Inbound** (External to Portal) | `grants.` | `grants.unity.acknowledgment` | Portal inbox queue (`grants.*.#`) |

This prevents the Portal from consuming its own outbound commands.

### Outbox Message Lifecycle

```
Pending → Processing → Published → Acknowledged   (happy path — ack received)
                                 → TimedOut        (no ack within threshold)
                                 → TimedOut → Acknowledged  (late ack after timeout)
                     → Failed                      (publish failed after max retries)
```

**Late ack safety**: If an ack arrives after a message is already `TimedOut`, it is still processed. The cache was already invalidated by the timeout job, so the next read fetches fresh data — no stale data risk.

### All Failure Paths

| Scenario | PluginEvent Source | Invalidates Cache? |
|----------|-------------------|:---:|
| Outbox publish fails (retries exhausted) | `OutboxFailure` | ✅ |
| Published but no ack within threshold | `AckTimeout` | ✅ |
| FAILED ack received from external system | `InboxRejection` | ✅ |
| SUCCESS ack received | _(none)_ | ❌ |

### Background Jobs

| Job | Purpose | Default Schedule |
|-----|---------|------------------|
| **OutboxProcessorJob** | Publishes `Pending` outbox messages to RabbitMQ | Every 30s |
| **InboxProcessorJob** | Processes `Pending` inbox messages; closes outbox ack loop | Every 15s |
| **OutboxTimeoutJob** | Detects `Published` messages with no ack past threshold; records PluginEvent + marks `TimedOut` | Every 60s |
| **MessageCleanupJob** | Deletes terminal-status messages older than retention period | Hourly (1-min startup delay) |

The cleanup job only deletes messages in terminal statuses — never `Pending` or `Processing`. Uses `ExecuteDeleteAsync()` for efficient single-SQL bulk deletion.

---

## Outbound Message Flow (Portal to External)

1. Plugin calls `IMessagePublisher.PublishAsync(message)`
2. `OutboxMessagePublisher` serializes the message and writes an `OutboxMessage` row (status: `Pending`)
3. `OutboxProcessorJob` (Quartz, every N seconds):
   - Acquires distributed lock
   - Picks up pending messages in batches
   - Publishes each to RabbitMQ via `IRabbitMQPublisher`
   - The AMQP `message_id` property is set to the outbox `MessageId` so external systems can echo it back in acknowledgments
   - Routing key: `commands.{pluginId}.plugindata` (generated by `GenerateRoutingKey`)
   - Marks message as `Published` or `Failed`
4. External system consumer picks up the message from its own queue

### OutboxMessage Table Schema

| Column | Type | Description |
|--------|------|-------------|
| `Id` | `long` | Primary key (auto-increment) |
| `MessageId` | `Guid` | Unique message identifier (matches the original `PluginDataMessage.MessageId`) |
| `MessageType` | `string` | Message type for routing/deserialization (e.g. `"PluginDataMessage"`) |
| `Payload` | `string` | Full JSON-serialized message body |
| `PluginId` | `string?` | ID of the plugin that created this message (e.g. `"UNITY"`) |
| `CorrelationId` | `string?` | Correlation ID for tracing (e.g. `"profile-{profileId}"`) |
| `Status` | `OutboxMessageStatus` | Current lifecycle status (see below) |
| `CreatedAt` | `DateTime` | When the message was created (UTC) |
| `ProcessedAt` | `DateTime?` | When the message was published or permanently failed (UTC) |
| `RetryCount` | `int` | Number of publish attempts so far |
| `LastError` | `string?` | Error message from the most recent failed attempt |
| `LockToken` | `string?` | Distributed lock token (prevents duplicate processing across pods) |
| `LockExpiry` | `DateTime?` | When the lock expires |

### OutboxMessageStatus Values

| Value | Name | Meaning |
|-------|------|---------|
| `0` | **Pending** | Waiting to be published. Set on creation, or after a failed attempt when retries remain. |
| `1` | **Published** | Successfully published to RabbitMQ. Terminal state. |
| `2` | **Failed** | Permanently failed after max retries exhausted. Terminal state. |
| `3` | **Processing** | Currently locked by a job instance for publishing. |
| `4` | **TimedOut** | Published but no acknowledgment received within `AckTimeoutMinutes`. |
| `5` | **Acknowledged** | External system acknowledged the message (SUCCESS or FAILED). Terminal state. |

### Outbound Command Payload

All plugin write operations (contact create/edit/delete, address edit, org edit, etc.) use `PluginDataMessage`:

```json
{
  "messageId": "326a1072-bf82-4ee9-9daa-b76ca147fa50",
  "messageType": "PluginDataMessage",
  "createdAt": "2026-03-04T20:11:19Z",
  "correlationId": "profile-019b4788-d7a7-7c40-b25e-98a361adbbfc",
  "pluginId": "UNITY",
  "dataType": "CONTACT_SET_PRIMARY_COMMAND",
  "data": {
    "action": "SetContactAsPrimary",
    "contactId": "a437675a-d642-455c-b3e0-388d75e6203f",
    "profileId": "019b4788-d7a7-7c40-b25e-98a361adbbfc",
    "provider": "DGP",
    "subject": "Abad@idir"
  }
}
```

### Unity Command Types Currently Published

| `dataType` | Action | Published by |
|------------|--------|-------------|
| `CONTACT_CREATE_COMMAND` | Create a new contact | `Unity.Contacts.cs` |
| `CONTACT_EDIT_COMMAND` | Edit an existing contact | `Unity.Contacts.cs` |
| `CONTACT_SET_PRIMARY_COMMAND` | Set a contact as primary | `Unity.Contacts.cs` |
| `CONTACT_DELETE_COMMAND` | Delete a contact | `Unity.Contacts.cs` |
| `ADDRESS_EDIT_COMMAND` | Edit an address | `Unity.Addresses.cs` |
| `ADDRESS_SET_PRIMARY_COMMAND` | Set an address as primary | `Unity.Addresses.cs` |
| `ORGANIZATION_EDIT_COMMAND` | Edit an organization | `Unity.Organizations.cs` |

### Full Command Payload Examples

<details>
<summary><b>CONTACT_CREATE_COMMAND</b></summary>

```json
{
  "messageId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "messageType": "PluginDataMessage",
  "createdAt": "2026-03-04T20:11:19Z",
  "correlationId": "profile-019b4788-d7a7-7c40-b25e-98a361adbbfc",
  "pluginId": "UNITY",
  "dataType": "CONTACT_CREATE_COMMAND",
  "data": {
    "action": "CreateContact",
    "contactId": "c8d27b95-20fe-4ef4-ad1e-15fd99ced56b",
    "profileId": "019b4788-d7a7-7c40-b25e-98a361adbbfc",
    "provider": "DGP",
    "subject": "Abad@idir",
    "data": {
      "name": "Andre Goncalves",
      "email": "andre@example.com",
      "title": "Program Director",
      "contactType": "ApplicantProfile",
      "homePhoneNumber": null,
      "mobilePhoneNumber": "555 987-6543",
      "workPhoneNumber": "555 864-2100",
      "workPhoneExtension": "101",
      "role": "Executive",
      "isPrimary": true
    }
  }
}
```
</details>

<details>
<summary><b>CONTACT_EDIT_COMMAND</b></summary>

```json
{
  "messageId": "b2c3d4e5-f6a7-8901-bcde-f12345678901",
  "messageType": "PluginDataMessage",
  "createdAt": "2026-03-04T20:12:00Z",
  "correlationId": "profile-019b4788-d7a7-7c40-b25e-98a361adbbfc",
  "pluginId": "UNITY",
  "dataType": "CONTACT_EDIT_COMMAND",
  "data": {
    "action": "EditContact",
    "contactId": "a437675a-d642-455c-b3e0-388d75e6203f",
    "profileId": "019b4788-d7a7-7c40-b25e-98a361adbbfc",
    "provider": "DGP",
    "subject": "Abad@idir",
    "data": {
      "name": "Alex Johnson Updated",
      "email": "alex.johnson.updated@unity.gov",
      "title": "Senior Program Director",
      "contactType": "ApplicantProfile",
      "homePhoneNumber": "555 123-4567",
      "mobilePhoneNumber": "555 987-6543",
      "workPhoneNumber": "555 864-2100",
      "workPhoneExtension": "102",
      "role": "Executive",
      "isPrimary": true
    }
  }
}
```
</details>

<details>
<summary><b>CONTACT_SET_PRIMARY_COMMAND</b></summary>

```json
{
  "messageId": "326a1072-bf82-4ee9-9daa-b76ca147fa50",
  "messageType": "PluginDataMessage",
  "createdAt": "2026-03-04T20:11:19Z",
  "correlationId": "profile-019b4788-d7a7-7c40-b25e-98a361adbbfc",
  "pluginId": "UNITY",
  "dataType": "CONTACT_SET_PRIMARY_COMMAND",
  "data": {
    "action": "SetContactAsPrimary",
    "contactId": "a437675a-d642-455c-b3e0-388d75e6203f",
    "profileId": "019b4788-d7a7-7c40-b25e-98a361adbbfc",
    "provider": "DGP",
    "subject": "Abad@idir"
  }
}
```
</details>

<details>
<summary><b>CONTACT_DELETE_COMMAND</b></summary>

```json
{
  "messageId": "d4e5f6a7-b8c9-0123-def0-123456789abc",
  "messageType": "PluginDataMessage",
  "createdAt": "2026-03-04T20:13:00Z",
  "correlationId": "profile-019b4788-d7a7-7c40-b25e-98a361adbbfc",
  "pluginId": "UNITY",
  "dataType": "CONTACT_DELETE_COMMAND",
  "data": {
    "action": "DeleteContact",
    "contactId": "b5a01793-e247-48c7-8257-25b0ed239883",
    "profileId": "019b4788-d7a7-7c40-b25e-98a361adbbfc",
    "provider": "DGP",
    "subject": "Abad@idir"
  }
}
```
</details>

<details>
<summary><b>ADDRESS_EDIT_COMMAND</b></summary>

```json
{
  "messageId": "e5f6a7b8-c9d0-1234-ef01-23456789abcd",
  "messageType": "PluginDataMessage",
  "createdAt": "2026-03-04T20:14:00Z",
  "correlationId": "profile-019b4788-d7a7-7c40-b25e-98a361adbbfc",
  "pluginId": "UNITY",
  "dataType": "ADDRESS_EDIT_COMMAND",
  "data": {
    "action": "EditAddress",
    "addressId": "AAD12E34-6789-0ABC-DEF1-234567890ABC",
    "profileId": "019b4788-d7a7-7c40-b25e-98a361adbbfc",
    "provider": "DGP",
    "subject": "Abad@idir",
    "data": {
      "addressType": "Physical",
      "street": "1234 Government Street",
      "street2": "Suite 600",
      "unit": "",
      "city": "Victoria",
      "province": "BC",
      "postalCode": "V8W1A4",
      "country": "",
      "isPrimary": true
    }
  }
}
```
</details>

<details>
<summary><b>ADDRESS_SET_PRIMARY_COMMAND</b></summary>

```json
{
  "messageId": "f6a7b8c9-d0e1-2345-f012-3456789abcde",
  "messageType": "PluginDataMessage",
  "createdAt": "2026-03-04T20:15:00Z",
  "correlationId": "profile-019b4788-d7a7-7c40-b25e-98a361adbbfc",
  "pluginId": "UNITY",
  "dataType": "ADDRESS_SET_PRIMARY_COMMAND",
  "data": {
    "action": "SetAddressAsPrimary",
    "addressId": "BBD12E34-6789-0ABC-DEF1-234567890ABC",
    "profileId": "019b4788-d7a7-7c40-b25e-98a361adbbfc",
    "provider": "DGP",
    "subject": "Abad@idir"
  }
}
```
</details>

<details>
<summary><b>ORGANIZATION_EDIT_COMMAND</b></summary>

```json
{
  "messageId": "a7b8c9d0-e1f2-3456-0123-456789abcdef",
  "messageType": "PluginDataMessage",
  "createdAt": "2026-03-04T20:16:00Z",
  "correlationId": "profile-019b4788-d7a7-7c40-b25e-98a361adbbfc",
  "pluginId": "UNITY",
  "dataType": "ORGANIZATION_EDIT_COMMAND",
  "data": {
    "action": "EditOrganization",
    "organizationId": "7AEF7815-27D3-5E9C-9686-68E6F36C51EA",
    "profileId": "019b4788-d7a7-7c40-b25e-98a361adbbfc",
    "provider": "DGP",
    "subject": "Abad@idir",
    "data": {
      "name": "Unity Government Solutions Updated",
      "organizationType": "Government Department",
      "organizationNumber": "UGS001234",
      "status": "Active",
      "nonRegOrgName": "Unity Tech Division",
      "fiscalMonth": "Apr",
      "fiscalDay": 1,
      "organizationSize": 150
    }
  }
}
```
</details>

### RabbitMQ Properties Set by Publisher

| Property | Value |
|----------|-------|
| `Type` | `"PluginDataMessage"` |
| `MessageId` | Unique GUID |
| `CorrelationId` | e.g. `"profile-{profileId}"` |
| `ContentType` | `"application/json"` |
| `Persistent` | `true` |

---

## Inbound Message Flow (External to Portal)

1. External system publishes a `MessageAcknowledgment` to the `grants.messaging` exchange
2. Routing key: `grants.{pluginId}.acknowledgment` (e.g. `grants.unity.acknowledgment`)
3. Portal's `RabbitMQConsumer` receives it (queue bound to `grants.*.#`)
4. Stores as `InboxMessage` (with duplicate detection)
5. `InboxProcessorJob` picks it up, routes via `PluginMessageRouter`
6. `PluginMessageRouter` looks up `IPluginMessageHandler` by `PluginId` and calls `HandleAcknowledgmentAsync`

### InboxMessage Table Schema

| Column | Type | Description |
|--------|------|-------------|
| `Id` | `long` | Primary key (auto-increment) |
| `MessageId` | `Guid` | Unique message identifier (used for duplicate detection) |
| `MessageType` | `string` | Message type (e.g. `"MessageAcknowledgment"`, `"PluginDataMessage"`) |
| `Payload` | `string` | Full JSON-serialized message body |
| `CorrelationId` | `string?` | Correlation ID for tracing (matches the original outbound command) |
| `Status` | `InboxMessageStatus` | Current lifecycle status (see below) |
| `ReceivedAt` | `DateTime` | When the message was received from RabbitMQ (UTC) |
| `ProcessedAt` | `DateTime?` | When processing completed or permanently failed (UTC) |
| `RetryCount` | `int` | Number of processing attempts so far |
| `LastError` | `string?` | Error message from the most recent failed attempt |
| `LockToken` | `string?` | Distributed lock token (prevents duplicate processing across pods) |
| `LockExpiry` | `DateTime?` | When the lock expires |

### InboxMessageStatus Values

| Value | Name | Meaning |
|-------|------|---------|
| `0` | **Pending** | Waiting to be processed. Set on receipt, or after a failed attempt when retries remain. |
| `1` | **Processed** | Successfully processed by the plugin handler. Terminal state. |
| `2` | **Failed** | Permanently failed after max retries exhausted. Terminal state. |
| `3` | **Processing** | Currently locked by a job instance for processing. |
| `4` | **Duplicate** | Duplicate message detected (same `MessageId` already exists). Terminal state. |

### Inbound Acknowledgment Payload

```json
{
  "messageId": "f1e2d3c4-b5a6-9870-fedc-ba9876543210",
  "messageType": "MessageAcknowledgment",
  "createdAt": "2026-03-04T20:11:20Z",
  "correlationId": "profile-019b4788-d7a7-7c40-b25e-98a361adbbfc",
  "pluginId": "UNITY",
  "originalMessageId": "326a1072-bf82-4ee9-9daa-b76ca147fa50",
  "status": "SUCCESS",
  "details": "Contact set as primary successfully",
  "processedAt": "2026-03-04T20:11:20Z"
}
```

### Acknowledgment `status` Values

| Status | Meaning | Portal Handler Action |
|--------|---------|----------------------|
| `SUCCESS` | Command processed successfully | Outbox message marked `Acknowledged`. No PluginEvent. |
| `FAILED` | Command could not be processed | Outbox message marked `Acknowledged`. PluginEvent recorded (`InboxRejection` / Error) which invalidates the relevant cache segment. |
| `PROCESSING` | Command received, still working | Log info, no further action (wait for final ack) |

---

## External System Consumer Contract

This section documents **what an external system must implement** to participate in the messaging pattern. Use this as a spec when building the real Unity consumer (or any other plugin's external system).

### 1. Consume Commands

**Queue setup:**
- Declare your own queue (e.g. `unity.commands`)
- Bind to exchange `grants.messaging` (topic) with routing key `commands.{yourPluginId}.plugindata`
- Use `autoAck: false` (manual acknowledgment after processing)

**Message you receive:**
- `BasicProperties.Type` = `"PluginDataMessage"`
- `BasicProperties.MessageId` = unique GUID (use this as `originalMessageId` in your ack)
- `BasicProperties.CorrelationId` = correlation ID (pass through in your ack)
- Body = JSON `PluginDataMessage` with `dataType` indicating the action

**Processing:**
1. Parse the JSON body
2. Read `dataType` to determine the action (e.g. `CONTACT_CREATE_COMMAND`)
3. Read `data` for the action payload
4. Process the command in your system
5. Publish an acknowledgment (see below)
6. BasicAck the delivery

### 2. Publish Acknowledgments

After processing a command, publish an acknowledgment back to the same exchange.

**Routing key:** `grants.{yourPluginId}.acknowledgment` (e.g. `grants.unity.acknowledgment`)

**Required AMQP properties:**

| Property | Value |
|----------|-------|
| `Type` | `"MessageAcknowledgment"` |
| `MessageId` | New unique GUID |
| `CorrelationId` | Same value from the incoming command |
| `ContentType` | `"application/json"` |
| `ContentEncoding` | `"utf-8"` |
| `Persistent` | `true` |

**Required JSON body:**

```json
{
  "messageId": "NEW-GUID",
  "messageType": "MessageAcknowledgment",
  "createdAt": "2026-03-04T20:11:19Z",
  "correlationId": "profile-019b4788-...",
  "pluginId": "UNITY",
  "originalMessageId": "MESSAGE-ID-FROM-INCOMING-COMMAND",
  "status": "SUCCESS",
  "details": "Contact set as primary successfully",
  "processedAt": "2026-03-04T20:11:19Z"
}
```

### 3. Acknowledgment Status Values

| Status | Meaning | When to use |
|--------|---------|-------------|
| `SUCCESS` | Command processed successfully | Happy path |
| `FAILED` | Command could not be processed | Validation error, system error, etc. |
| `PROCESSING` | Command received, still working | Long-running operations (optional) |

### 4. Error Handling

- If processing fails, still publish a `FAILED` acknowledgment with error details
- Always BasicAck the RabbitMQ delivery (don't leave messages unacked)
- The Portal will route the FAILED ack to the plugin handler for compensation logic

---

## Plugin-Agnostic Design

The entire inbox/outbox infrastructure is **generic** and routes by `PluginId`. Here's what makes it plugin-agnostic:

| Component | Plugin-specific? | How it routes |
|-----------|-----------------|---------------|
| `OutboxMessage` / `InboxMessage` | No - stores any plugin's messages | `PluginId` column |
| `OutboxProcessorJob` | No - publishes all pending messages | Routing key includes `PluginId` |
| `RabbitMQConsumer` | No - consumes from single inbox queue | Stores all incoming messages |
| `InboxProcessorJob` | No - processes all pending messages | Delegates to `PluginMessageRouter` |
| `PluginMessageRouter` | No - resolves handler by `PluginId` | `IPluginMessageHandler.PluginId` |
| `IPluginMessageHandler` | **Yes** - one per plugin | Registered in DI |

### Adding a New Plugin to the Messaging System

To add a plugin called `NEWPLUGIN`:

**Step 1: Publish commands from your plugin**

```csharp
public partial class NewPlugin(
    ILogger<NewPlugin> logger,
    IMessagePublisher? messagePublisher = null) : IProfilePlugin
{
    public string PluginId => "NEWPLUGIN";

    private async Task FireSomeCommand(Guid entityId, ProfileContext ctx, CancellationToken ct)
    {
        if (messagePublisher == null)
            throw new InvalidOperationException("Message publisher is required");

        var message = new PluginDataMessage(
            PluginId,
            "ENTITY_UPDATE_COMMAND",
            new
            {
                Action = "UpdateEntity",
                EntityId = entityId,
                ctx.ProfileId,
                ctx.Provider
            },
            correlationId: $"profile-{ctx.ProfileId}");

        await messagePublisher.PublishAsync(message, ct);
    }
}
```

This will be published to RabbitMQ with routing key `commands.newplugin.plugindata`.

**Step 2: Create a message handler**

```csharp
public class NewPluginMessageHandler : IPluginMessageHandler
{
    private readonly ILogger<NewPluginMessageHandler> _logger;

    public NewPluginMessageHandler(ILogger<NewPluginMessageHandler> logger)
    {
        _logger = logger;
    }

    public string PluginId => "NEWPLUGIN";

    public async Task<Result> HandleAcknowledgmentAsync(
        MessageAcknowledgment acknowledgment, MessageContext context)
    {
        switch (acknowledgment.Status.ToUpperInvariant())
        {
            case "SUCCESS":
                _logger.LogInformation("NEWPLUGIN: Command {Id} succeeded", 
                    acknowledgment.OriginalMessageId);
                // TODO: Update cache, notify user, etc.
                break;
            case "FAILED":
                _logger.LogWarning("NEWPLUGIN: Command {Id} failed: {Details}", 
                    acknowledgment.OriginalMessageId, acknowledgment.Details);
                // TODO: Retry, compensate, alert, etc.
                break;
            case "PROCESSING":
                _logger.LogInformation("NEWPLUGIN: Command {Id} still processing", 
                    acknowledgment.OriginalMessageId);
                break;
        }
        return Result.Success();
    }

    public async Task<Result> HandleIncomingDataAsync(
        PluginDataMessage message, MessageContext context)
    {
        _logger.LogInformation("NEWPLUGIN: Received data of type {DataType}", message.DataType);
        // TODO: Handle data pushed from external system
        return Result.Success();
    }

    public bool CanHandle(IMessage message)
    {
        return message.PluginId != null &&
               message.PluginId.Equals(PluginId, StringComparison.OrdinalIgnoreCase);
    }
}
```

**Step 3: Register in DI**

In `MessagingServiceExtensions.cs`:

```csharp
services.AddScoped<IPluginMessageHandler, NewPluginMessageHandler>();
```

**Step 4: Configure the external system's queue**

The external system binds its own queue to exchange `grants.messaging` with routing key `commands.newplugin.plugindata`, and publishes acks back with routing key `grants.newplugin.acknowledgment`.

**That's it.** The outbox, inbox, Quartz jobs, RabbitMQ publisher/consumer, and router all work automatically.

---

## DEMO vs UNITY Plugin Comparison

The two existing plugins use different strategies intentionally:

| Aspect | DEMO | UNITY |
|--------|------|-------|
| **Purpose** | Self-contained mock for development | Real distributed integration |
| **Write operations** | Direct (in-memory + Redis cache) | Event-driven (outbox + RabbitMQ) |
| **Cache strategy** | Synchronous — writes to in-memory store, then persists to Redis | Optimistic — patches cached JSON, then fires async command |
| **Uses `IMessagePublisher`?** | Only for profile population notification | Yes, for all write commands |
| **Uses RabbitMQ?** | No | Yes |
| **Has `IPluginMessageHandler`?** | Yes (logs only, scaffolding) | Yes (handles acks from external system) |
| **Ack/Nack flow?** | Not needed (writes are synchronous) | Full round-trip via inbox |
| **External system?** | None | Real Unity instance + RabbitMQ |
| **`primaryContactId` / `primaryAddressId` in responses?** | Yes — resolved from Redis cache | Yes — resolved from optimistically-patched cache |

---

## Optimistic Cache Update Strategy

The UNITY plugin uses an **optimistic cache update** pattern for all write operations. Because commands are asynchronous (outbox → RabbitMQ → external system), the UI needs to see changes immediately without waiting for the external system to process the command.

### How it works

```
1. Plugin reads cached ProfileData from IPluginCacheService.TryGetAsync()
2. Patches the JSON data in-memory (add/edit/remove items, toggle flags)
3. Writes the patched data back via IPluginCacheService.SetAsync()
4. Fires the command to the outbox (async, eventually consistent)
5. Returns success to the caller — UI sees changes immediately
```

### Operations and their cache effects

| Operation | Cache Patch | Primary Flag Rules |
|-----------|-------------|----------------------|
| **Contact Create** | Appends new contact to `contacts` array | If `isPrimary: true`, clears `isPrimary` on all existing contacts |
| **Contact Edit** | Replaces matching contact in `contacts` array | If `isPrimary: true`, clears `isPrimary` on all other contacts |
| **Contact SetPrimary** | Toggles `isPrimary` on all contacts | Target gets `true`, all others get `false` |
| **Contact Delete** | Removes contact from `contacts` array | If deleted contact was primary, auto-promotes the most recently created remaining contact (by `creationTime`; falls back to first remaining if none have a `creationTime`) |
| **Address Edit** | Replaces matching address in `addresses` array | If `isPrimary: true`, clears `isPrimary` on all other addresses |
| **Address SetPrimary** | Toggles `isPrimary` on all addresses | Target gets `true`, all others get `false` |
| **Organization Edit** | Patches `OrganizationInfo` object | — |

### Shared helpers (`Unity.CacheHelpers.cs`)

| Helper | Purpose |
|--------|---------|
| `PatchCachedProfileDataAsync()` | Reads cache → applies transform → writes back. No-op if cache is empty. |
| `RebuildWithArray()` | Rebuilds a JSON object, replacing a named array with a callback-built array. |
| `RebuildWithObject()` | Rebuilds a JSON object, replacing a named object property with a patched version. |

### Cache key format

```
{CacheKeyPrefix}{profileId}:{pluginId}:{provider}:{dataKey}
Example: profile:019b4788-...:UNITY:DGP:CONTACTINFO
```

### Eventually consistent

The cache is patched optimistically. When the cache TTL expires (default 60 min absolute, 15 min sliding), the next read will fetch fresh data from the external API, reconciling any drift.

---

## Configuration

### Portal-side (appsettings.json)

```json
{
  "Messaging": {
    "RabbitMQ": {
      "HostName": "localhost",
      "Port": 5672,
      "UserName": "guest",
      "Password": "guest",
      "VirtualHost": "/",
      "UseSsl": false,
      "ConnectionTimeout": "00:00:30",
      "RetryCount": 3,
      "RetryDelay": "00:00:30"
    },
    "Outbox": {
      "PollingIntervalSeconds": 15,
      "BatchSize": 100,
      "MaxRetries": 3,
      "RetentionDays": 14,
      "CleanupIntervalHours": 24,
      "AckTimeoutMinutes": 5,
      "AckTimeoutPollingIntervalSeconds": 60
    },
    "Inbox": {
      "PollingIntervalSeconds": 15,
      "BatchSize": 50,
      "MaxRetries": 3,
      "RetentionDays": 14,
      "CleanupIntervalHours": 24
    },
    "DistributedLocks": {
      "DefaultTimeoutMinutes": 5,
      "RenewalIntervalMinutes": 2,
      "WaitTimeoutSeconds": 5
    },
    "BackgroundJobs": {
      "MaxConcurrency": 3,
      "Enabled": true,
      "MisfireThresholdSeconds": 60,
      "BaseBackoffSeconds": 15,
      "MaxBackoffSeconds": 300,
      "BackoffMultiplier": 2.0,
      "LogEveryNthFailure": 20,
      "StartupDelaySeconds": 60
    }
  }
}
```

### Configuration Reference

| Section | Key | Default | Description |
|---------|-----|---------|-------------|
| `RabbitMQ` | `HostName` | `localhost` | RabbitMQ server hostname |
| | `Port` | `5672` | AMQP port |
| | `UserName` / `Password` | `guest` | Credentials |
| | `VirtualHost` | `/` | Virtual host |
| | `UseSsl` | `false` | Enable TLS |
| | `ConnectionTimeout` | `00:00:30` | Connection timeout |
| | `RetryCount` | `3` | Retry attempts for failed connections |
| | `RetryDelay` | `00:00:30` | Delay between retries |
| `Outbox` | `PollingIntervalSeconds` | `30` | How often `OutboxProcessorJob` runs |
| | `BatchSize` | `100` | Messages per batch |
| | `MaxRetries` | `5` | Max publish attempts before `Failed` |
| | `RetentionDays` | `7` | Days to keep published/failed messages |
| | `CleanupIntervalHours` | `24` | How often `MessageCleanupJob` runs |
| | `AckTimeoutMinutes` | `5` | Minutes after publishing before unacknowledged messages are timed out. Set to `0` to disable. |
| | `AckTimeoutPollingIntervalSeconds` | `60` | How often `OutboxTimeoutJob` checks for stale published messages |
| `Inbox` | `PollingIntervalSeconds` | `15` | How often `InboxProcessorJob` runs |
| | `BatchSize` | `50` | Messages per batch |
| | `MaxRetries` | `3` | Max processing attempts before `Failed` |
| | `RetentionDays` | `7` | Days to keep processed/failed messages |
| | `CleanupIntervalHours` | `24` | How often cleanup runs |
| `DistributedLocks` | `DefaultTimeoutMinutes` | `5` | Lock duration for job processing |
| | `RenewalIntervalMinutes` | `2` | Lock renewal frequency |
| | `WaitTimeoutSeconds` | `5` | Max wait to acquire a lock |
| `BackgroundJobs` | `MaxConcurrency` | CPU count | Max concurrent Quartz jobs |
| | `Enabled` | `true` | Enable/disable all background jobs |
| | `MisfireThresholdSeconds` | `60` | Quartz misfire tolerance |
| | `BaseBackoffSeconds` | `15` | Initial delay (seconds) before retrying after a job failure |
| | `MaxBackoffSeconds` | `300` | Maximum backoff cap (5 minutes) — delay stops growing beyond this |
| | `BackoffMultiplier` | `2.0` | Exponential multiplier per consecutive failure (15→30→60→120→300) |
| | `LogEveryNthFailure` | `20` | During sustained failures, emit a Warning log every Nth failure |
| | `StartupDelaySeconds` | `60` | Delay before cleanup job starts after application boot |

### External System — Unity (appsettings.json)

The UNITY application must declare its own inbound queue, bind to the exchange, and publish acknowledgments back. See `UNITY-RabbitMQ-Integration-Spec.md` for the full contract.

```json
{
  "RabbitMQ": {
    "HostName": "localhost",
    "Port": 5672,
    "UserName": "guest",
    "Password": "guest",
    "VirtualHost": "/",
    "Exchange": "grants.messaging",
    "ExchangeType": "topic",
    "InboundQueue": "unity.commands",
    "InboundRoutingKeys": [ "commands.unity.plugindata" ],
    "AckRoutingKey": "grants.unity.acknowledgment"
  }
}
```

---

## Message Tracking & Monitoring

### Outbox Monitoring
```sql
-- Check outbound message status by plugin
SELECT "PluginId", "MessageType", "Status", COUNT(*) as "Count"
FROM "OutboxMessages"
GROUP BY "PluginId", "MessageType", "Status";

-- Pending messages waiting to be published
SELECT * FROM "OutboxMessages"
WHERE "Status" = 0  -- Pending
ORDER BY "CreatedAt";

-- Currently processing (locked)
SELECT * FROM "OutboxMessages"
WHERE "Status" = 3  -- Processing
AND "LockExpiry" > NOW();

-- Permanently failed messages (exhausted retries)
SELECT * FROM "OutboxMessages"
WHERE "Status" = 2  -- Failed
ORDER BY "ProcessedAt" DESC;

-- Messages stuck in processing (lock expired)
SELECT * FROM "OutboxMessages"
WHERE "Status" = 3  -- Processing
AND "LockExpiry" < NOW();
```

### Inbox Monitoring
```sql
-- Check inbound message status
SELECT "MessageType", "Status", COUNT(*) as "Count"
FROM "InboxMessages"
GROUP BY "MessageType", "Status";

-- Pending acknowledgments waiting to be processed
SELECT * FROM "InboxMessages"
WHERE "Status" = 0  -- Pending
ORDER BY "ReceivedAt";

-- Successfully processed
SELECT * FROM "InboxMessages"
WHERE "Status" = 1  -- Processed
ORDER BY "ProcessedAt" DESC
LIMIT 20;

-- Failed after max retries
SELECT * FROM "InboxMessages"
WHERE "Status" = 2  -- Failed
ORDER BY "ProcessedAt" DESC;

-- Duplicates detected
SELECT * FROM "InboxMessages"
WHERE "Status" = 4  -- Duplicate
ORDER BY "ReceivedAt" DESC;

-- Trace a command end-to-end using CorrelationId
SELECT 'OUTBOX' as "Source", "MessageId", "MessageType", "Status", "CreatedAt", "ProcessedAt"
FROM "OutboxMessages"
WHERE "CorrelationId" = 'profile-019b4788-d7a7-7c40-b25e-98a361adbbfc'
UNION ALL
SELECT 'INBOX', "MessageId", "MessageType", "Status", "ReceivedAt", "ProcessedAt"
FROM "InboxMessages"
WHERE "CorrelationId" = 'profile-019b4788-d7a7-7c40-b25e-98a361adbbfc'
ORDER BY "CreatedAt";
```

---

## Error Handling & Reliability

### Job Circuit Breaker (Exponential Backoff)

All background jobs (`OutboxProcessorJob`, `InboxProcessorJob`, `MessageCleanupJob`) are protected by a shared `JobCircuitBreaker` singleton that prevents log flooding and wasted work when infrastructure is unavailable (e.g. missing DB tables, connection failures).

**How it works:**
- Quartz triggers continue firing at their configured interval (e.g. every 15s)
- On each tick, the job asks `JobCircuitBreaker.ShouldExecute(jobKey)` before doing any real work
- If the backoff period hasn't elapsed, the job returns immediately (no DB calls, no error logs)
- On success, the breaker resets to zero failures
- On failure, the breaker computes an exponential delay: `BaseBackoffSeconds × BackoffMultiplier^(failures-1)`, capped at `MaxBackoffSeconds`

**Logging strategy:**
| Failure # | Log Level | Content |
|-----------|-----------|---------|
| 1st | `Error` | Full exception with stack trace |
| 2nd–19th | `Debug` | Suppressed — only failure count and next retry time |
| Every 20th | `Warning` | Periodic reminder with failure count and error message |
| Recovery | `Information` | "Circuit recovered after N consecutive failures" |

**Backoff progression (with defaults):**
| Consecutive Failures | Delay Before Next Attempt |
|---------------------|--------------------------|
| 1 | 15 seconds |
| 2 | 30 seconds |
| 3 | 60 seconds |
| 4 | 120 seconds |
| 5+ | 300 seconds (5 min cap) |

**Configuration:** All values are tunable under `Messaging:BackgroundJobs` in `appsettings.json` — see [Configuration Reference](#configuration-reference) above.

### Message Delivery Guarantees
- **At-least-once delivery** for outbound messages (outbox retry)
- **Duplicate detection** for inbound messages (inbox checks `MessageId`)
- **Configurable retry** with max retry count for failed messages
- **Distributed locking** prevents duplicate processing across pods

### Failure Scenarios
1. **Database Down**: Messages queued in RabbitMQ until service recovery. Job circuit breaker backs off to 5-minute intervals, preventing log flooding.
2. **Missing Tables (Migration Issue)**: Jobs detect failure on first attempt, circuit opens, backoff escalates to 5 minutes. Only 1 error log + periodic warnings instead of thousands.
3. **RabbitMQ Down**: Messages remain in outbox for retry when restored (simulation mode fallback)
4. **Plugin Error**: Message marked as failed with retry logic
5. **External System Down**: Acknowledgments delayed but command remains tracked in outbox

---

## Plugin Event System

The plugin event system provides a **general-purpose, persistent notification channel** between the backend and the user. Events can be informational messages, warnings, or errors — they are not limited to failures.

### Use Cases

| Severity | Example | Compensation? |
|----------|---------|---------------|
| **Info** | "Your profile has been synced with the external system" | No |
| **Info** | "A new grant program is available for your organization" | No |
| **Warning** | "Your submission is approaching the deadline" | No |
| **Error** | "The external system rejected your contact creation: duplicate detected" | Yes — cache invalidated |
| **Error** | "Your address update could not be sent to the external system" | Yes — cache invalidated |

### Architecture

```
Any caller (plugin, handler, job)
        |
        v
  IPluginEventService.RecordAsync(PluginEventContext)
        |
        +---> Persists PluginEvent to database
        |
        +---> If severity == Error:
                Invalidate cache segment via IPluginCommandMetadataRegistry
                (so next read fetches fresh data from external API)
```

Existing callers that record error events automatically:

| Caller | Trigger | Source |
|--------|---------|--------|
| `OutboxProcessorJob` | Message permanently fails to publish (max retries) | `OutboxFailure` |
| `InboxProcessorJob` | External system returns a FAILED acknowledgment | `InboxRejection` |
| `OutboxTimeoutJob` | Published message receives no ack within `AckTimeoutMinutes` | `AckTimeout` |

### PluginEvents Table Schema

| Column | Type | Description |
|--------|------|-------------|
| `Id` | `long` | Primary key (auto-increment) |
| `EventId` | `Guid` | Unique event identifier (public, used in API) |
| `ProfileId` | `Guid` | Which user this event is for |
| `PluginId` | `string` | Which plugin raised the event (e.g. `"UNITY"`) |
| `Provider` | `string` | Which provider (e.g. `"DGP"`) |
| `DataType` | `string` | Context — command type or event category (e.g. `"CONTACT_CREATE_COMMAND"`, `"DEADLINE_WARNING"`) |
| `EntityId` | `string?` | Affected entity (e.g. contactId, addressId) |
| `Severity` | `PluginEventSeverity` | `Info (0)`, `Warning (1)`, `Error (2)` |
| `Source` | `PluginEventSource` | What triggered the event (see below) |
| `UserMessage` | `string` | Human-readable message safe for UI display |
| `TechnicalDetails` | `string?` | Detailed error/diagnostic info for logs |
| `OriginalMessageId` | `Guid?` | Links to outbox/inbox message (if applicable) |
| `CorrelationId` | `string?` | For tracing |
| `IsAcknowledged` | `bool` | User dismissed this event |
| `CreatedAt` | `DateTime` | When the event was created (UTC) |
| `AcknowledgedAt` | `DateTime?` | When user dismissed |

### PluginEventSeverity Values

| Value | Name | Compensation? |
|-------|------|---------------|
| `0` | **Info** | No |
| `1` | **Warning** | No |
| `2` | **Error** | Yes — cache segment invalidated |

### PluginEventSource Values

| Value | Name | Description |
|-------|------|-------------|
| `0` | **OutboxFailure** | Outbox message failed to publish after max retries |
| `1` | **InboxRejection** | External system returned a FAILED acknowledgment |
| `2` | **ExternalNotification** | External system sent a notification or status update |
| `3` | **System** | System-generated event (e.g. cache refresh, background job) |
| `4` | **Plugin** | Event raised by plugin-specific business logic |
| `5` | **AckTimeout** | Published message received no acknowledgment within the configured timeout |

### API Endpoints

| Method | Route | Description |
|--------|-------|-------------|
| `GET` | `/Events/{PluginId}/{Provider}` | Returns unacknowledged events for the current user |
| `PATCH` | `/Events/{EventId}/acknowledge` | Acknowledges (dismisses) a single event owned by the current user. Returns `404` if the event does not exist or belongs to another user |
| `PATCH` | `/Events/{PluginId}/{Provider}/acknowledge-all` | Acknowledges all events for plugin/provider |

**Example response** (`GET /Events/UNITY/DGP`):

```json
{
  "events": [
    {
      "eventId": "abc-123",
      "severity": "Error",
      "source": "InboxRejection",
      "dataType": "CONTACT_CREATE_COMMAND",
      "entityId": "c8d27b95-...",
      "userMessage": "The external system rejected your contact creation: duplicate detected. Your data may revert on next refresh.",
      "createdAt": "2026-03-04T20:11:20Z",
      "isAcknowledged": false
    },
    {
      "eventId": "def-456",
      "severity": "Info",
      "source": "ExternalNotification",
      "dataType": "PROGRAM_UPDATE",
      "entityId": null,
      "userMessage": "A new grant program 'Digital Innovation Fund' is now available.",
      "createdAt": "2026-03-04T18:00:00Z",
      "isAcknowledged": false
    }
  ]
}
```

### Recording Events from Plugin Code

```csharp
// Info event — no compensation
await pluginEventService.RecordAsync(new PluginEventContext(
    profileId,
    "UNITY",
    "DGP",
    "PROGRAM_UPDATE",
    EntityId: null,
    PluginEventSeverity.Info,
    PluginEventSource.ExternalNotification,
    "A new grant program 'Digital Innovation Fund' is now available."),
    cancellationToken);

// Error event — triggers cache invalidation automatically
await pluginEventService.RecordFailureAsync(new PluginEventContext(
    profileId,
    "UNITY",
    "DGP",
    "CONTACT_CREATE_COMMAND",
    entityId,
    PluginEventSeverity.Error,
    PluginEventSource.InboxRejection,
    "The external system rejected your contact creation.",
    TechnicalDetails: "Duplicate key violation"),
    cancellationToken);
```

### Extensibility — `IPluginCommandMetadataProvider`

Each plugin registers a metadata provider that maps its command types to cache segments and friendly names. The event system uses this registry for compensation, keeping the core service plugin-agnostic.

```csharp
// A new plugin just registers its provider — everything else works automatically
services.AddSingleton<IPluginCommandMetadataProvider, NewPluginCommandMetadataProvider>();
```

### Plugin Event Monitoring

```sql
-- Active (unacknowledged) events by severity
SELECT "PluginId", "Severity", COUNT(*) as "Count"
FROM "PluginEvents"
WHERE "IsAcknowledged" = false
GROUP BY "PluginId", "Severity";

-- Recent error events
SELECT "EventId", "PluginId", "DataType", "UserMessage", "CreatedAt"
FROM "PluginEvents"
WHERE "Severity" = 2  -- Error
ORDER BY "CreatedAt" DESC
LIMIT 20;

-- Events for a specific user
SELECT * FROM "PluginEvents"
WHERE "ProfileId" = '019b4788-d7a7-7c40-b25e-98a361adbbfc'
AND "IsAcknowledged" = false
ORDER BY "CreatedAt" DESC;
```

---

## Development Setup

### Prerequisites

The UNITY plugin integration requires both **RabbitMQ** and a **local UNITY instance** running:

1. **RabbitMQ**: `docker run -d --name rabbitmq -p 5672:5672 -p 15672:15672 rabbitmq:3-management`
2. **UNITY application**: Running locally (see UNITY team documentation for setup instructions)
3. **Portal API**: `dotnet run --project src/Grants.ApplicantPortal.API.Web`

### End-to-end flow

1. Trigger a write operation (e.g. edit a contact via UNITY plugin)
2. Watch logs: outbox publish → RabbitMQ → Unity consume → ack publish → inbox receive → handler processes

RabbitMQ Management UI: http://localhost:15672 (guest/guest)