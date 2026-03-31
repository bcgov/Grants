# Unity Grant Manager Integration

## Overview

The Applicant Portal integrates with Unity Grant Manager through two mechanisms:

1. **REST API** - Synchronous queries (provider/tenant discovery, profile data population)
2. **RabbitMQ Messaging** - Asynchronous write operations (contact/address/org management commands and acknowledgments)

This document provides a high-level overview of both integration points.

> **Detailed Technical Documentation**: For complete API specifications, authentication details, and implementation guidance, see the [Unity Grant Manager Integration Guide](https://github.com/bcgov/Unity/blob/main/applications/Unity.GrantManager/docs/ApplicantPortalIntegration.md)

## Quick Start

### 1. Obtain API Key
Contact the Unity Grant Manager team to obtain an API key for your environment (dev/test/prod).

### 2. Configure API Key
Store the API key securely in your application configuration:

```json
{
  "UnityIntegration": {
    "BaseUrl": "https://unity.example.com",
    "ApiKey": "your-api-key-here"
  }
}
```

### 3. Make API Requests
Include the API key in the `X-API-Key` header:

```http
GET /api/app/applicant-profiles/tenants?ProfileId=abc&Subject=user@idp
Host: unity.example.com
X-API-Key: your-api-key-here
```

## Read Operations (REST API)

### 1. Get Applicant Tenants (Providers)
Retrieves the list of grant programs (tenants) an applicant has access to based on their OIDC identity. In the portal, these are exposed as **providers** for the UNITY plugin.

**Portal endpoint**: `GET /Plugins/UNITY/providers`

**Unity endpoint**: `GET /api/app/applicant-profiles/tenants`

**Parameters**:
- `ProfileId`: Your internal user identifier
- `Subject`: OIDC subject from user's authentication token

**Response**:
```json
[
  {
    "tenantId": "guid",
    "tenantName": "Housing Grant Program"
  },
  {
    "tenantId": "guid",
    "tenantName": "Business Development Fund"
  }
]
```

Results are cached per profile. Empty results are not cached so the next request retries the upstream API.

### 2. Profile Data Population
When a user accesses contacts, addresses, organizations, submissions, or payments for a UNITY provider, the portal calls the Unity REST API to hydrate the cache on first access. Subsequent reads are served from cache.

**Portal endpoints** (all follow the same hydration pattern):
- `GET /Contacts/UNITY/{Provider}` - Contacts
- `GET /Addresses/UNITY/{Provider}` - Addresses
- `GET /Organizations/UNITY/{Provider}` - Organizations
- `GET /Submissions/UNITY/{Provider}` - Submissions
- `GET /Payments/UNITY/{Provider}` - Payments

### 3. Contact Roles
The UNITY plugin defines the following contact role options (used for role selectors in the UI):

| Key | Label |
|-----|-------|
| General | General |
| Primary | Primary Contact |
| Financial | Financial Officer |
| SigningAuthority | Additional Signing Authority |
| Executive | Executive |

**Portal endpoint**: `GET /Contacts/UNITY/roles`

## How It Works

### Architecture Flow

```
User Logs In (OIDC/IDP)
       |
       v
Applicant Portal (extracts OIDC Subject)
       |  API Request (Subject + API Key)
       v
Unity Grant Manager (queries tenant mapping)
       |  Returns Available Tenants
       v
Applicant Portal (displays programs as providers)
       |  User views/edits data for a provider
       v
Read: REST API call to Unity -> cache result
Write: Update cache + publish command to RabbitMQ -> Unity processes + acks
```

### Data Synchronization

The Unity Grant Manager maintains a centralized lookup table (AppApplicantTenantMaps) that maps OIDC subjects to tenants:

**Real-time Updates**:
- When an applicant submits an application, Unity automatically creates/updates the mapping
- Mappings are available within milliseconds

**Nightly Reconciliation**:
- A background job runs daily at 2 AM PST to ensure data consistency
- Catches any missed events or new tenants

**Result**: The API always returns up-to-date tenant associations without requiring expensive database scans.

## Authentication

All API requests require an API key in the `X-API-Key` header.

**Security Requirements**:
- Always use HTTPS in production
- Store API key in secure configuration (Azure Key Vault, environment variables)
- Never commit API keys to source control
- Implement key rotation strategy

## Error Handling

| Status Code | Meaning | Action |
|-------------|---------|--------|
| 200 OK | Success | Process response |
| 400 Bad Request | Invalid parameters | Check query parameters |
| 401 Unauthorized | Invalid API key | Verify API key configuration |
| 500 Internal Server Error | Server error | Retry with exponential backoff |

**Empty Result**: When a user has no tenant associations, the API returns an empty array `[]` with status 200 OK.

## Subject Identifier Format

The Unity system normalizes OIDC subject identifiers by:
1. Extracting the identifier before the `@` symbol
2. Converting to uppercase

**Examples**:
- `smzfrrla7j5hw6z7wzvyzdrtq6dj6fbr@chefs-frontend-5299` becomes `SMZFRRLA7J5HW6Z7WZVYZDRTQ6DJ6FBR`
- `anonymous@bcservicescard` becomes `ANONYMOUS`

**Implementation Note**: Pass the full OIDC subject from your authentication token. Unity handles the normalization internally.

## Write Operations (Contact, Address, Organization Management)

The Applicant Portal supports full CRUD operations for contacts, address editing, and organization editing against Unity. These operations go through the following flow:

1. **API endpoint** receives the request (e.g., `POST /Contacts/UNITY/{Provider}`)
2. **Use case handler** calls the Unity plugin
3. **Unity plugin** updates the local cache optimistically and publishes a command to RabbitMQ via the transactional outbox
4. **Unity Grant Manager** processes the command and sends an acknowledgment back

### Supported Write Commands

| Operation | API Endpoint | Command (dataType) |
|-----------|-------------|----------------------|
| Create Contact | `POST /Contacts/UNITY/{Provider}` | CONTACT_CREATE_COMMAND |
| Edit Contact | `PUT /Contacts/{ContactId}/UNITY/{Provider}` | CONTACT_EDIT_COMMAND |
| Delete Contact | `DELETE /Contacts/{ContactId}/UNITY/{Provider}` | CONTACT_DELETE_COMMAND |
| Set Primary Contact | `PATCH /Contacts/{ContactId}/UNITY/{Provider}/set-primary` | CONTACT_SET_PRIMARY_COMMAND |
| Edit Address | `PUT /Addresses/{AddressId}/UNITY/{Provider}` | ADDRESS_EDIT_COMMAND |
| Set Primary Address | `PATCH /Addresses/{AddressId}/UNITY/{Provider}/set-primary` | ADDRESS_SET_PRIMARY_COMMAND |
| Edit Organization | `PUT /Organizations/{OrgId}/UNITY/{Provider}` | ORGANIZATION_EDIT_COMMAND |

### Read-Only Data

These data types are fetched from Unity via REST API and cached. No write endpoints exist for them yet:

| Data Type | API Endpoint |
|-----------|-------------|
| Submissions | `GET /Submissions/UNITY/{Provider}` |
| Payments | `GET /Payments/UNITY/{Provider}` |

## RabbitMQ Messaging

### Overview

Write operations from the Portal to Unity are communicated asynchronously via RabbitMQ using the **Transactional Inbox/Outbox** pattern. This ensures reliable, at-least-once delivery with full audit trail.

### Message Flow

**Outbound (Portal to Unity):**
```
Write endpoint -> Use Case -> Plugin updates cache + saves OutboxMessage (Pending)
  -> OutboxProcessorJob (every 30s) -> Publish to RabbitMQ
  -> Routing key: commands.unity.plugindata
  -> Unity consumer processes command
```

**Inbound (Unity to Portal):**
```
Unity publishes acknowledgment -> routing key: grants.unity.acknowledgment
  -> Portal RabbitMQConsumer -> InboxMessage table
  -> InboxProcessorJob (every 15s) -> marks outbox message Acknowledged
  -> If FAILED ack: records PluginEvent + invalidates cache
```

### Failure Handling

| Scenario | Effect |
|----------|--------|
| Outbox publish fails (retries exhausted) | PluginEvent (OutboxFailure) recorded, cache invalidated |
| Published but no ack within threshold | PluginEvent (AckTimeout) recorded, cache invalidated |
| FAILED ack received from Unity | PluginEvent (InboxRejection) recorded, cache invalidated |
| SUCCESS ack received | No action needed, cache remains valid |

When a PluginEvent is recorded, the user can see it via `GET /Events/UNITY/{Provider}` and dismiss it via the acknowledge endpoints.

### Background Jobs

| Job | Purpose | Schedule |
|-----|---------|----------|
| OutboxProcessorJob | Publishes pending outbox messages to RabbitMQ | Every 30s |
| InboxProcessorJob | Processes pending inbox messages; closes ack loop | Every 15s |
| OutboxTimeoutJob | Detects published messages with no ack past threshold | Every 60s |
| MessageCleanupJob | Deletes terminal-status messages older than retention period | Hourly |

### Testing

**Local Development**:
```bash
# Start RabbitMQ with Docker
docker run -d --name rabbitmq -p 5672:5672 -p 15672:15672 rabbitmq:3-management
# Access Management UI at http://localhost:15672 (admin/admin)
```

> **Detailed Documentation**: For complete message schemas, payload examples, outbox/inbox lifecycle, and external system consumer contract, see:
> - [Messaging Plugin Integration Guide](Messaging-Plugin-Integration-Guide.md) - Full portal-side messaging architecture
> - [UNITY RabbitMQ Integration Spec](UNITY-RabbitMQ-Integration-Spec.md) - External consumer contract for Unity

## Performance

- **Response Time**: < 100ms typical (< 50ms p50)
- **Rate Limits**: Contact Unity team for rate limit details
- **Availability**: 99.9% uptime SLA

## Support and Contacts

**Unity Grant Manager Team**:
- Repository: https://github.com/bcgov/Unity
- Technical Documentation: [Unity Integration Guide](https://github.com/bcgov/Unity/blob/main/applications/Unity.GrantManager/docs/ApplicantPortalIntegration.md)

**For Questions**:
- Slack: #unity-grant-manager (internal)
- Email: unity-support@gov.bc.ca

## Deployment Checklist

- [ ] API key obtained for each environment (dev/test/prod)
- [ ] API key stored securely (Key Vault, environment variables)
- [ ] Base URLs configured per environment
- [ ] RabbitMQ connectivity configured per environment
- [ ] Error handling implemented
- [ ] Logging configured (exclude API keys from logs)
- [ ] Rate limiting handled
- [ ] Integration tested in dev environment
- [ ] Plugin events verified (ack timeout, failure scenarios)

## Change Log

| Date | Version | Changes |
|------|---------|---------|
| 2026-01-XX | 1.0.0 | Initial integration documentation |
| 2026-03-XX | 2.0.0 | Added write operations (contact/address/org CRUD), updated RabbitMQ messaging with actual implemented commands, added plugin events documentation |

## Related Documentation

- [API Endpoints](API-Endpoints.md) - Complete REST endpoint reference
- [Plugin Architecture](Plugin-Architecture.md) - Plugin system design
- [Messaging Plugin Integration Guide](Messaging-Plugin-Integration-Guide.md) - Outbox/inbox messaging details
- [UNITY RabbitMQ Integration Spec](UNITY-RabbitMQ-Integration-Spec.md) - External consumer contract
- [Authentication](Authentication.md) - Keycloak OIDC configuration
- [Secrets Management](Secrets-Management.md) - Configuration and secrets
