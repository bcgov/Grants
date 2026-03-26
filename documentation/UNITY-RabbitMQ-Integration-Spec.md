# UNITY Application — RabbitMQ Integration Specification

> **Purpose:** This document describes exactly how the real UNITY application must integrate with the Grants Applicant Portal messaging system via RabbitMQ. Hand this to the AI agent working on the UNITY codebase.

---

## 1. RabbitMQ Topology

| Component | Value |
|---|---|
| **Exchange** | `grants.messaging` |
| **Exchange Type** | `topic` |
| **Exchange Durable** | `true` |
| **UNITY Inbound Queue** | `unity.commands` (UNITY declares and owns this) |
| **Inbound Routing Key Binding** | `commands.unity.plugindata` |
| **Ack Routing Key (outbound from UNITY)** | `grants.unity.acknowledgment` |
| **Message Content-Type** | `application/json` (UTF-8) |
| **Message Persistence** | `true` (durable) |
| **Prefetch** | `1` recommended |

### Direction Diagram

```
Portal Outbox ──► grants.messaging exchange ──► commands.unity.plugindata ──► [UNITY inbound queue]
                                                                               │
                                              grants.unity.acknowledgment  ◄───┘
                                                       │
                                              grants.messaging.inbox ◄─── Portal Consumer
```

The Portal outbox publishes commands with routing keys prefixed `commands.unity.*`. The Portal's own consumer binds to `grants.*.#` and `system.*.#`, so it will NOT consume its own outbound commands. UNITY consumes from `commands.unity.*` and publishes acknowledgments back on `grants.unity.acknowledgment`, which the Portal consumer picks up.

---

## 2. Messages UNITY Will RECEIVE (Consume)

Every inbound command arrives as a `PluginDataMessage`. The routing key will be `commands.unity.plugindata`.

### AMQP Properties on Inbound Messages

| AMQP Property | Value |
|---|---|
| `type` | `"PluginDataMessage"` |
| `content_type` | `"application/json"` |
| `content_encoding` | `"utf-8"` |
| `persistent` | `true` |
| `message_id` | GUID string — **identical** to the `messageId` inside the JSON body. Use either source for the ack's `originalMessageId`. |
| `correlation_id` | `"profile-{profileId}"` format |
| `timestamp` | Unix epoch seconds |

### JSON Envelope Structure (camelCase)

```json
{
  "messageId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "messageType": "PluginDataMessage",
  "createdAt": "2025-01-15T10:30:00Z",
  "correlationId": "profile-{profileId}",
  "pluginId": "UNITY",
  "dataType": "CONTACT_CREATE_COMMAND",
  "data": {
    "action": "CreateContact",
    "contactId": "guid",
    "profileId": "guid",
    "provider": "string",
    "data": { ... }
  }
}
```

> **IMPORTANT:** The `dataType` field is the command discriminator — NOT `messageType` (which is always `"PluginDataMessage"`). Route your internal handling based on `dataType`.

> **IMPORTANT:** Entity fields are nested inside `data.data`. The top-level `data` object contains metadata (`action`, `profileId`, `provider`, `subject`, entity IDs). The actual entity fields are one level deeper.

---

### 2a. `CONTACT_CREATE_COMMAND`

```json
{
  "messageId": "guid",
  "messageType": "PluginDataMessage",
  "createdAt": "2025-01-15T10:30:00Z",
  "correlationId": "profile-{profileId}",
  "pluginId": "UNITY",
  "dataType": "CONTACT_CREATE_COMMAND",
  "data": {
    "action": "CreateContact",
    "contactId": "guid",
    "profileId": "guid",
    "provider": "string (e.g. PROGRAM1)",
    "subject": "string (OIDC subject, e.g. Abad@idir)",
    "data": {
      "name": "string",
      "email": "string",
      "title": "string | null",
      "contactType": "string (e.g. PRIMARY, GRANTS)",
      "homePhoneNumber": "string | null",
      "mobilePhoneNumber": "string | null",
      "workPhoneNumber": "string | null",
      "workPhoneExtension": "string | null",
      "role": "string | null",
      "isPrimary": false
    }
  }
}
```

### 2b. `CONTACT_EDIT_COMMAND`

```json
{
  "dataType": "CONTACT_EDIT_COMMAND",
  "data": {
    "action": "EditContact",
    "contactId": "guid",
    "profileId": "guid",
    "provider": "string",
    "subject": "string (OIDC subject)",
    "data": {
      "name": "string",
      "email": "string",
      "title": "string | null",
      "contactType": "string",
      "homePhoneNumber": "string | null",
      "mobilePhoneNumber": "string | null",
      "workPhoneNumber": "string | null",
      "workPhoneExtension": "string | null",
      "role": "string | null",
      "isPrimary": false
    }
  }
}
```

### 2c. `CONTACT_SET_PRIMARY_COMMAND`

```json
{
  "dataType": "CONTACT_SET_PRIMARY_COMMAND",
  "data": {
    "action": "SetContactAsPrimary",
    "contactId": "guid",
    "profileId": "guid",
    "provider": "string",
    "subject": "string (OIDC subject)"
  }
}
```

### 2d. `CONTACT_DELETE_COMMAND`

```json
{
  "dataType": "CONTACT_DELETE_COMMAND",
  "data": {
    "action": "DeleteContact",
    "contactId": "guid",
    "profileId": "guid",
    "provider": "string",
    "subject": "string (OIDC subject)"
  }
}
```

### 2e. `ADDRESS_EDIT_COMMAND`

```json
{
  "dataType": "ADDRESS_EDIT_COMMAND",
  "data": {
    "action": "EditAddress",
    "addressId": "guid",
    "profileId": "guid",
    "provider": "string",
    "subject": "string (OIDC subject)",
    "data": {
      "addressType": "string (e.g. MAILING, PHYSICAL)",
      "street": "string",
      "street2": "string | null",
      "unit": "string | null",
      "city": "string",
      "province": "string",
      "postalCode": "string",
      "country": "string | null",
      "isPrimary": false
    }
  }
}
```

### 2f. `ADDRESS_SET_PRIMARY_COMMAND`

```json
{
  "dataType": "ADDRESS_SET_PRIMARY_COMMAND",
  "data": {
    "action": "SetAddressAsPrimary",
    "addressId": "guid",
    "profileId": "guid",
    "provider": "string",
    "subject": "string (OIDC subject)"
  }
}
```

### 2g. `ORGANIZATION_EDIT_COMMAND`

```json
{
  "dataType": "ORGANIZATION_EDIT_COMMAND",
  "data": {
    "action": "EditOrganization",
    "organizationId": "guid",
    "profileId": "guid",
    "provider": "string",
    "subject": "string (OIDC subject)",
    "data": {
      "name": "string",
      "organizationType": "string | null",
      "organizationNumber": "string | null",
      "status": "string | null",
      "nonRegOrgName": "string | null",
      "fiscalMonth": "string | null",
      "fiscalDay": "integer | null (1-31)",
      "organizationSize": "integer | null (0-999999999)"
    }
  }
}
```

---

## 3. Messages UNITY Must SEND BACK (Acknowledgments)

After processing **every** command, UNITY **must** publish a `MessageAcknowledgment` back to the same `grants.messaging` exchange.

### Routing Key

`grants.unity.acknowledgment`

### AMQP Properties to Set

| Property | Value |
|---|---|
| `type` | `"MessageAcknowledgment"` |
| `content_type` | `"application/json"` |
| `content_encoding` | `"utf-8"` |
| `persistent` | `true` |
| `message_id` | New GUID (unique for the ack itself) |
| `correlation_id` | **Pass through** from the original inbound message |
| `timestamp` | Unix epoch seconds |

### JSON Payload (camelCase)

```json
{
  "messageId": "new-guid-for-this-ack",
  "messageType": "MessageAcknowledgment",
  "createdAt": "2025-01-15T10:30:05Z",
  "correlationId": "profile-{profileId}",
  "pluginId": "UNITY",
  "originalMessageId": "guid-from-the-inbound-command",
  "status": "SUCCESS",
  "details": "Contact created successfully",
  "processedAt": "2025-01-15T10:30:05Z"
}
```

### Status Values the Portal Understands

| Status | Portal Behavior |
|---|---|
| `SUCCESS` | Logs success. No user-visible action needed — the Portal already applied an optimistic cache update when the command was first submitted by the user. |
| `FAILED` | Records a `PluginEvent` (error severity) visible to the user as a notification banner. Invalidates the relevant cache segment so the next read fetches fresh data from UNITY's API, effectively **reverting** the optimistic update. Include a meaningful `details` string — it is shown to the user. |
| `PROCESSING` | Logs that the message is still being handled. No immediate user-visible action. Use this for long-running operations. |

---

## 4. Critical Implementation Rules

### 4.1 Always Send an Acknowledgment

Even on processing failure, publish a `FAILED` acknowledgment and then ACK the RabbitMQ delivery. **Do not** leave messages stuck in the queue or nack without requeue. The Portal needs the ack to know the outcome.

### 4.2 Guard Against Acknowledgment Loops

The `grants.messaging` exchange is **shared**. If UNITY's queue accidentally binds to `grants.*.#` (instead of only `commands.unity.*`), it will receive its own acknowledgment messages back. Always check:

```
if messageType == "MessageAcknowledgment":
    ack delivery and discard — do NOT process
```

### 4.3 Preserve `correlationId`

Always pass through the `correlationId` from the inbound command to the outbound acknowledgment. The Portal uses it (format: `profile-{guid}`) as a fallback to determine which user profile a failure belongs to if payload parsing fails.

### 4.4 Route on `dataType`, NOT `messageType`

`messageType` is always `"PluginDataMessage"` for all commands. The actual command discriminator is `dataType` inside the JSON body.

### 4.5 Entity Fields Are Nested

The structure is `data.data` — the outer `data` has metadata (action, IDs, provider), the inner `data` has the actual entity fields (name, email, street, etc.).

### 4.6 `originalMessageId` Must Match

The `originalMessageId` in the acknowledgment must be the `messageId` from the inbound command (available both from the JSON body and the AMQP `message_id` property). The Portal uses this to correlate acks with the original outbox message for failure tracking.

---

## 5. Minimal Consumer Pseudocode

```
on startup:
  connect to RabbitMQ
  declare exchange "grants.messaging" (topic, durable) — idempotent
  declare queue "unity.commands" (durable)
  bind queue → exchange with routing key "commands.unity.plugindata"
  set prefetch = 1
  start consuming (manual ack)

on message received(delivery):
  messageId   = delivery.properties.message_id  OR parse from body
  messageType = delivery.properties.type         OR parse from body
  correlationId = delivery.properties.correlation_id

  // Guard: skip ack messages to prevent infinite loops
  if messageType == "MessageAcknowledgment":
    ack delivery
    return

  body = parse JSON(delivery.body)
  dataType = body.dataType

  try:
    switch (dataType):
      "CONTACT_CREATE_COMMAND"      → create contact in UNITY DB using body.data
      "CONTACT_EDIT_COMMAND"        → update contact
      "CONTACT_SET_PRIMARY_COMMAND" → set primary flag
      "CONTACT_DELETE_COMMAND"      → delete/deactivate contact
      "ADDRESS_EDIT_COMMAND"        → update address
      "ADDRESS_SET_PRIMARY_COMMAND" → set primary flag
      "ORGANIZATION_EDIT_COMMAND"   → update organization
      default                       → log unknown command type

    publish acknowledgment:
      exchange      = "grants.messaging"
      routing_key   = "grants.unity.acknowledgment"
      status        = "SUCCESS"
      details       = "description of what was done"
      originalMessageId = messageId from inbound
      correlationId     = pass through from inbound

  catch error:
    publish acknowledgment:
      status  = "FAILED"
      details = error.message (this is shown to the Portal user)

  ack delivery (always, even on failure — ack was already published)
```

---

## 6. RabbitMQ Connection Configuration

```json
{
  "RabbitMQ": {
    "HostName": "your-rabbitmq-host",
    "Port": 5672,
    "UserName": "unity-service-account",
    "Password": "secret",
    "VirtualHost": "/",
    "Exchange": "grants.messaging",
    "ExchangeType": "topic",
    "InboundQueue": "unity.commands",
    "InboundRoutingKeys": [ "commands.unity.plugindata" ],
    "AckRoutingKey": "grants.unity.acknowledgment"
  }
}
```

For local development against the Portal's docker-compose environment, use:

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

## 7. What Happens on the Portal Side (Context for UNITY Developers)

Understanding the Portal's behavior helps explain why each rule above matters:

### Optimistic Updates

When a user submits a change (e.g. edit contact), the Portal:
1. **Immediately updates its local cache** with the new data (optimistic update)
2. **Queues a command** in the outbox table (database)
3. **Returns success** to the user — the UI reflects the change instantly

A background job (`OutboxProcessorJob`) picks up the queued message and publishes it to RabbitMQ. UNITY receives and processes it.

### On `SUCCESS` Acknowledgment

Nothing visible happens — the optimistic update was already correct. The Portal logs success.

### On `FAILED` Acknowledgment

The Portal:
1. Looks up the original outbox message to extract context (profileId, dataType, entityId)
2. Records a **PluginEvent** (error severity) in the database
3. **Invalidates the cache segment** for that data type (e.g. `CONTACTINFO`, `ADDRESSINFO`, `ORGINFO`)
4. On the user's next page load, a **notification banner** appears showing the `details` string from the ack
5. The next data read goes directly to UNITY's REST API (cache was invalidated), effectively **reverting** to the real state

This is why the `details` field in a `FAILED` ack should be **user-friendly** — it's displayed directly to the user.

### Cache Segment Mapping

The Portal maps `dataType` → cache segment as follows:

| dataType | Cache Segment |
|---|---|
| `CONTACT_CREATE_COMMAND` | `CONTACTINFO` |
| `CONTACT_EDIT_COMMAND` | `CONTACTINFO` |
| `CONTACT_SET_PRIMARY_COMMAND` | `CONTACTINFO` |
| `CONTACT_DELETE_COMMAND` | `CONTACTINFO` |
| `ADDRESS_EDIT_COMMAND` | `ADDRESSINFO` |
| `ADDRESS_SET_PRIMARY_COMMAND` | `ADDRESSINFO` |
| `ORGANIZATION_EDIT_COMMAND` | `ORGINFO` |

---

## 8. Testing

### Against the Portal locally

1. Start the Portal backend (with RabbitMQ running)
2. Start UNITY's consumer pointing at the same RabbitMQ instance
3. Use the Portal UI to create/edit/delete a contact, address, or organization
4. Observe the command arriving in UNITY's consumer logs
5. Observe the acknowledgment arriving in the Portal's inbox processor logs

### Verifying the exchange and queues

```bash
# List exchanges
rabbitmqctl list_exchanges | grep grants

# List queues
rabbitmqctl list_queues | grep -E "grants|unity"

# List bindings
rabbitmqctl list_bindings | grep -E "grants|unity|commands"
```

### Simulating a FAILED ack

Temporarily force your consumer to always return `FAILED` status. Then trigger a change from the Portal UI. You should see:
- A PluginEvent recorded in the Portal database (`plugin_events` table)
- The cache for that data type invalidated
- A notification banner on the user's next page load

---

## 9. Reference Implementation

The `UNITY-RabbitMQ-Integration-Spec.md` (this document) describes the full contract that the UNITY application must implement. The UNITY consumer should:

- Connect to RabbitMQ with retry/backoff
- Declare the exchange and its own inbound queue (`unity.commands`)
- Consume commands from routing key `commands.unity.plugindata`
- Guard against acknowledgment loops
- Process commands against the UNITY database
- Publish `MessageAcknowledgment` back on `grants.unity.acknowledgment`
