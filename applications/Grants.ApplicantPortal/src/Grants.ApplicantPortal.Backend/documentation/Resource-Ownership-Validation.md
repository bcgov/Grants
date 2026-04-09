# Resource Ownership Validation

## Overview

The Grants Applicant Portal enforces **server-side resource ownership validation** on all write operations (create, edit, delete, set-primary) to prevent **IDOR (Insecure Direct Object Reference)** attacks. This ensures that authenticated users can only modify resources that belong to their profile, even if they manipulate client-supplied IDs in HTTP requests.

---

## Problem Statement

All write endpoints accept client-supplied IDs (`applicantId`, `contactId`, `addressId`, `organizationId`) in the request body or route. Without server-side validation, an authenticated user could:

1. **Inject a random `applicantId`** to create contacts/addresses under another user's profile
2. **Inject a random `contactId`/`addressId`** to edit or delete another user's data
3. **Bypass the `isEditable` flag** to modify resources linked to submitted applications

These are standard IDOR vulnerabilities that authentication alone cannot prevent.

---

## Solution: `IResourceOwnershipValidator`

A service-layer validator (`IResourceOwnershipValidator`) sits between the management services and plugins. It cross-references client-supplied IDs against the authenticated user's cached profile data before allowing operations to proceed.

### Architecture

```
Request → ProfileResolutionMiddleware (JWT → ProfileId)
        → FastEndpoint
        → MediatR Handler
        → Management Service (ContactManagementService, AddressManagementService, OrganizationManagementService)
            → IResourceOwnershipValidator ← validates IDs against cached profile data
            → Plugin (only if validation passes)
```

### Key Design Decisions

| Decision | Rationale |
|----------|-----------|
| **Validation at management service layer** | Plugin-agnostic — works for both Unity and Demo plugins without changes |
| **Cache-based validation** | Profile data is already cached by `IPluginCacheService`; no additional API calls needed |
| **Fail-closed** | If cache is empty and hydration fails, the request is rejected |
| **Hydration fallback** | If cached data is missing, attempts to populate from the plugin before rejecting |
| **Profile-scoped cache keys** | Cache keys include `profileId` (from JWT), making cross-user data access impossible at the cache level |

---

## Validation Methods

### `ValidateApplicantOwnershipAsync(applicantId, profileContext)`

Used on **create** operations. Checks that the supplied `applicantId` exists in the user's cached contact data:
- Checks the top-level `applicantId` property
- Searches the `contacts[]` array for any contact with a matching `applicantId`

### `ValidateContactOwnershipAsync(contactId, profileContext)`

Used on **edit, delete, set-primary** operations. Searches the cached `contacts[]` array for a contact with the matching `contactId`. Returns both ownership status and `isEditable` flag.

### `ValidateAddressOwnershipAsync(addressId, profileContext)`

Used on **edit, delete, set-primary** operations. Searches the cached `addresses[]` array for an address with the matching `addressId`. Returns both ownership status and `isEditable` flag.

### `ValidateOrganizationOwnershipAsync(organizationId, profileContext)`

Used on **edit** operations. Checks the cached organization data:
- Checks the `organizationInfo` object for a matching `organizationId`
- Searches the `organizations[]` array if present

---

## Cache Segments

The validator reads from these cache segments (scoped to `{profileId}:{pluginId}:{provider}`):

| Segment | Used By | Data Structure |
|---------|---------|---------------|
| `{provider}:CONTACTINFO` | Contact + Applicant validation | `{ applicantId, contacts: [{ contactId, isEditable, applicantId }] }` |
| `{provider}:ADDRESSINFO` | Address validation | `{ addresses: [{ addressId, isEditable }] }` |
| `{provider}:ORGINFO` | Organization validation | `{ organizationInfo: { organizationId } }` or `{ organizations: [{ organizationId }] }` |

---

## Response Behavior

| Validation Result | HTTP Status | Result Type | Description |
|-------------------|-------------|-------------|-------------|
| Not owned | `403 Forbidden` | `Result.Forbidden()` | Resource does not belong to the user |
| Owned but not editable | `400 Bad Request` | `Result.Invalid()` | Resource is linked to a submission |
| Cache miss + hydration failure | `403 Forbidden` | `Result.Forbidden()` | Fail-closed: cannot verify ownership |
| Owned and editable | — | Proceeds to plugin | Normal operation |

---

## Editability Enforcement

Some resources have an `isEditable` flag that indicates whether they can be modified (e.g., contacts/addresses linked to a submitted application are read-only). The validator enforces this server-side:

- **Edit** and **Delete** operations check `isEditable` after confirming ownership
- **Set-primary** operations only check ownership (primary designation is always changeable)
- **Create** operations only check `applicantId` ownership (new resources don't have editability constraints)

If `isEditable` is missing from the cached data, it defaults to `true` (backward compatibility).

---

## Implementation Files

| File | Layer | Purpose |
|------|-------|---------|
| `Core/Services/IResourceOwnershipValidator.cs` | Core | Interface + `OwnershipValidationResult` record |
| `UseCases/Security/ResourceOwnershipValidator.cs` | UseCases | Implementation with cache lookup + hydration |
| `Infrastructure/Services/ContactManagementService.cs` | Infrastructure | Calls validator before contact operations |
| `Infrastructure/Services/AddressManagementService.cs` | Infrastructure | Calls validator before address operations |
| `Infrastructure/Services/OrganizationManagementService.cs` | Infrastructure | Calls validator before organization edit |
| `Core/Features/Security/SecurityEventTypes.cs` | Core | `ResourceOwnershipFailure` event type constant |

---

## Test Coverage

| Test File | Tests | Focus |
|-----------|-------|-------|
| `ResourceOwnershipValidatorTests.cs` | 15 | Cache parsing, ownership detection, editability, hydration fallback, fail-closed behavior |
| `ContactManagementServiceSecurityTests.cs` | 4 | Forbidden on ownership failure, Invalid on editability failure, plugin never called |
| `AddressManagementServiceSecurityTests.cs` | 4 | Same patterns as contact tests for address operations |

---

## Related Documentation

- [API Endpoints](API-Endpoints.md) — Endpoint reference with authorization details
- [Plugin Architecture](Plugin-Architecture.md) — Plugin system design and request flow
- [Messaging Plugin Integration Guide](Messaging-Plugin-Integration-Guide.md) — Outbox/inbox messaging pattern
