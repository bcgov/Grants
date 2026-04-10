# API Endpoints Reference

Complete reference for all REST endpoints in the Grants Applicant Portal API. All endpoints use [FastEndpoints](https://fast-endpoints.com/) and JWT Bearer authentication via Keycloak unless otherwise noted.

---

## Authentication

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| GET | `/auth/callback` | Anonymous | OAuth Authorization Code callback — exchanges auth code for JWT tokens (server-side flow) |
| GET | `/Auth/userinfo` | ✅ | Returns current user info from JWT claims (subject, username, email, roles) |

> **Note:** The main UI uses PKCE flow and does **not** use `/auth/callback`. That endpoint exists for server-side OAuth flows, API testing, and legacy integrations.

---

## System

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| GET | `/System/health` | Anonymous | Health check — returns system status including plugin registry initialization. Returns `503` if unhealthy. |
| GET | `/System/plugins` | Anonymous | Lists all enabled plugins with their IDs, descriptions, and supported features. |

---

## Plugins

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| GET | `/Plugins/{PluginId}/providers` | ✅ | Returns available providers for a plugin. For UNITY this calls the upstream tenants API; for DEMO it returns hardcoded programs. Each provider includes an `id`, `name`, and optional `metaData` dictionary. |

---

## Contacts

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| GET | `/Contacts/{PluginId}/{Provider}` | ✅ | Retrieve contacts with automatic cache hydration |
| POST | `/Contacts/{PluginId}/{Provider}` | ✅ | Create a new contact |
| PUT | `/Contacts/{ContactId}/{PluginId}/{Provider}` | ✅ | Update an existing contact |
| DELETE | `/Contacts/{ContactId}/{PluginId}/{Provider}` | ✅ | Delete an existing contact |
| PATCH | `/Contacts/{ContactId}/{PluginId}/{Provider}/set-primary` | ✅ | Set a contact as primary |
| GET | `/Contacts/{PluginId}/roles` | ✅ | Retrieve available contact role options for a plugin |

### Retrieve Contacts

Returns cached contact data. If the cache is empty, automatically hydrates from the plugin and returns the result. Includes cache stampede protection.

**Response codes:** `200` success, `400` invalid request, `401` unauthorized, `403` forbidden (ownership validation failure), `404` not found

### Create Contact

Creates a new contact. The request body includes `name` (required), `email`, `title`, `role` (required), phone numbers, `isPrimary`, and `applicantId` (required, Guid). The current user's profile is resolved from the JWT automatically.

For UNITY, a `CONTACT_CREATE_COMMAND` message is published to RabbitMQ after the local cache is updated.

**Response:** `{ contactId, name, primaryContactId }`

### Update Contact

Updates an existing contact identified by `ContactId`. Same body fields as create (including `applicantId`).

For UNITY, a `CONTACT_EDIT_COMMAND` message is published.

**Response:** `{ contactId, message, primaryContactId }`

### Delete Contact

Deletes a contact by `ContactId`. This endpoint expects a **JSON request body** containing `applicantId` (required, Guid).

For UNITY, a `CONTACT_DELETE_COMMAND` message is published.

**Response:** `{ contactId, message, primaryContactId }`

### Set Contact as Primary

Sets a contact as the primary contact for the profile. This endpoint expects a **JSON request body** containing `applicantId` (required, Guid).

For UNITY, a `CONTACT_SET_PRIMARY_COMMAND` message is published.

**Response:** `{ contactId, message, primaryContactId }`

### Retrieve Contact Roles

Returns the list of available role options (key/label pairs) defined by the plugin. Used to populate role selectors in the UI.

**Example response:**
```json
{
  "pluginId": "UNITY",
  "roles": [
    { "key": "General", "label": "General" },
    { "key": "Primary", "label": "Primary Contact" },
    { "key": "Financial", "label": "Financial Officer" },
    { "key": "SigningAuthority", "label": "Additional Signing Authority" },
    { "key": "Executive", "label": "Executive" }
  ]
}
```

---

## Addresses

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| GET | `/Addresses/{PluginId}/{Provider}` | ✅ | Retrieve addresses with automatic cache hydration |
| POST | `/Addresses/{PluginId}/{Provider}` | ✅ | Create a new address |
| PUT | `/Addresses/{AddressId}/{PluginId}/{Provider}` | ✅ | Update an existing address |
| DELETE | `/Addresses/{AddressId}/{PluginId}/{Provider}` | ✅ | Delete an existing address |
| PATCH | `/Addresses/{AddressId}/{PluginId}/{Provider}/set-primary` | ✅ | Set an address as primary |

### Create Address

Creates a new address. Required fields: `addressType`, `street`, `city`, `province`, `postalCode`. Optional: `street2`, `unit`, `country`, `isPrimary`. The request body also includes `applicantId` (required, Guid).

For UNITY, an `ADDRESS_CREATE_COMMAND` message is published to RabbitMQ after the local cache is updated.

**Response:** `{ addressId, primaryAddressId }`

### Update Address

Updates an existing address. Required fields: `addressType`, `street`, `city`, `province`, `postalCode`. Optional: `street2`, `unit`, `country`, `isPrimary`. The request body also includes `applicantId` (required, Guid).

For UNITY, an `ADDRESS_EDIT_COMMAND` message is published.

**Response:** `{ addressId, message, primaryAddressId }`

### Delete Address

Deletes an address by `AddressId`. This endpoint expects a **JSON request body** containing `applicantId` (required, Guid).

For UNITY, an `ADDRESS_DELETE_COMMAND` message is published.

**Response:** `{ addressId, message, primaryAddressId }`

### Set Address as Primary

Sets an address as the primary address for the profile.

For UNITY, an `ADDRESS_SET_PRIMARY_COMMAND` message is published.

**Response:** `{ addressId, message, primaryAddressId }`

---

## Organizations

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| GET | `/Organizations/{PluginId}/{Provider}` | ✅ | Retrieve organizations with automatic cache hydration |
| PUT | `/Organizations/{OrganizationId}/{PluginId}/{Provider}` | ✅ | Update an existing organization |

### Update Organization

Updates an existing organization. Required fields: `name`, `organizationType`, `organizationNumber`, `status`, `nonRegOrgName`, `organizationSize`. Optional: `fiscalMonth`, `fiscalDay`.

For UNITY, an `ORGANIZATION_EDIT_COMMAND` message is published.

---

## Submissions

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| GET | `/Submissions/{PluginId}/{Provider}` | ✅ | Retrieve submissions with automatic cache hydration |

Read-only. Returns cached submission data for the profile/plugin/provider combination.

---

## Payments

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| GET | `/Payments/{PluginId}/{Provider}` | ✅ | Retrieve payments with automatic cache hydration |

Read-only. Returns cached payment data for the profile/plugin/provider combination.

---

## Events (Plugin Events)

Plugin events track messaging failures and other asynchronous issues that the user should be aware of (e.g., outbox publish failures, ack timeouts, rejected acknowledgments).

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| GET | `/Events/{PluginId}/{Provider}` | ✅ | Retrieve unacknowledged plugin events |
| PATCH | `/Events/{EventId}/acknowledge` | ✅ | Acknowledge (dismiss) a single event |
| PATCH | `/Events/{PluginId}/{Provider}/acknowledge-all` | ✅ | Acknowledge all events for a plugin/provider |

### Event DTO

```json
{
  "eventId": "guid",
  "severity": "Warning|Error|Info",
  "source": "OutboxFailure|AckTimeout|InboxRejection",
  "dataType": "CONTACT_EDIT_COMMAND",
  "entityId": "guid-of-affected-entity",
  "userMessage": "Your contact update could not be confirmed...",
  "createdAt": "2026-03-04T20:11:19Z",
  "isAcknowledged": false
}
```

---

## Common Patterns

### Route Parameters

All data endpoints use `{PluginId}` and `{Provider}` to scope requests to a specific plugin and provider (program). The `ProfileId` is **never** in the route — it is resolved automatically from the authenticated user's JWT via the `ProfileResolutionMiddleware`.

### Authorization & Resource Ownership

All write operations (create, edit, delete, set-primary) enforce **server-side resource ownership validation** in addition to JWT authentication. Client-supplied IDs (`applicantId`, `contactId`, `addressId`, `organizationId`) are cross-referenced against the authenticated user's cached profile data before the operation is allowed to proceed.

- **`403 Forbidden`** — The resource does not belong to the authenticated user (IDOR prevention)
- **`400 Bad Request`** — The resource is not editable (e.g., linked to a submitted application)

This validation is fail-closed: if the user's cached profile data cannot be loaded, the request is rejected. See [Resource Ownership Validation](Resource-Ownership-Validation.md) for implementation details.

### Cache Hydration

All `Retrieve*` (GET) endpoints follow the same pattern:
1. Check cache for existing data
2. If not cached, call the plugin to fetch/populate data
3. Store in cache and return
4. Cache stampede protection prevents concurrent hydration for the same key

### Write Operations + Messaging

All write endpoints (Create, Update, Delete, SetAsPrimary) follow this pattern:
1. Update the local cache optimistically
2. If the plugin supports messaging (UNITY), publish a command to RabbitMQ via the outbox
3. The external system processes the command and sends an acknowledgment
4. If the ack fails or times out, a PluginEvent is recorded and the cache is invalidated

See [Messaging Plugin Integration Guide](Messaging-Plugin-Integration-Guide.md) for the full messaging lifecycle.

---

## Related Documentation

- [Authentication](Authentication.md) — JWT/Keycloak configuration
- [Plugin Architecture](Plugin-Architecture.md) — Plugin system design
- [Resource Ownership Validation](Resource-Ownership-Validation.md) — IDOR prevention and ownership enforcement
- [Messaging Plugin Integration Guide](Messaging-Plugin-Integration-Guide.md) — RabbitMQ outbox/inbox pattern
- [API Access Patterns](API-Access-Patterns.md) — Frontend and direct API usage
