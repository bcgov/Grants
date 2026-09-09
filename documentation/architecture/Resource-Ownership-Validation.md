# Resource Ownership Validation

## Overview

The Grants Applicant Portal enforces **server-side resource ownership validation** on all write operations (create, edit, delete, set-primary) to prevent **IDOR (Insecure Direct Object Reference)** attacks. This ensures that authenticated users can only modify resources that belong to their profile, even if they manipulate client-supplied IDs in HTTP requests.

---

> Updated: `GET /Submissions/{PluginId}/{Provider}/{SubmissionId:Guid}/Form` introduces a second, read-side ownership check that is **not** implemented via `IResourceOwnershipValidator` (that service remains scoped to the write-operation flow described below). Instead, `RetrieveSubmissionFormQueryHandler` performs an ad hoc check inline: it re-fetches the caller's own `SUBMISSIONINFO` list and confirms the requested `SubmissionId` is present before ever fetching `SUBMISSIONFORM` (which can contain applicant PII/financial figures). A miss returns `Result.Forbidden()`, same as the validator-based flow. This is a narrower, single-purpose variant of the same "verify ownership before returning/mutating a client-supplied ID" principle, applied to a GET endpoint rather than a write operation.
>
> **⚠️ Unity applicant merging (provisional, pending Unity confirmation):** Unity now merges applicant records across different OIDC subjects belonging to the same real-world applicant (e.g. two different login methods), and its "root filtering" can cause `SUBMISSIONINFO` for one `Subject` to include submissions originally created under a *different, sibling* `Subject`/`ProfileId` of the same merged applicant — see [Unity-Integration.md § Applicant Merging Across OIDC Subjects](Unity-Integration.md#applicant-merging-across-oidc-subjects-root-filtering). Because `RetrieveSubmissionFormQueryHandler`'s check is simply "is this id present in whatever Unity returned for my `Subject`," this check will now pass for those sibling submissions too. That is very likely intentional (the caller genuinely is the same applicant), but it means "the caller's own `SUBMISSIONINFO` list" is no longer a strict single-sub boundary — it is whatever Unity decides to merge. The Portal has no independent way to distinguish "legitimately merged sibling" from "Unity bug/over-broad merge"; it fully trusts Unity's response.

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
| **Profile-scoped cache keys** | Cache keys include `profileId` (from JWT), making cross-user data access impossible at the cache level *for data the Portal itself scopes*. Note: this guarantee is only as strong as what Unity puts inside a given `profileId`'s cached response — Unity's applicant-merging/root-filtering behavior can intentionally include a sibling sub's data in that response (see the note above and [Unity-Integration.md](Unity-Integration.md#applicant-merging-across-oidc-subjects-root-filtering)), so "profile-scoped" no longer implies "single-sub-scoped" in all cases |

---

## Validation Methods

### `ValidateApplicantOwnershipAsync(applicantId, profileContext)`

Used on **create** operations. Checks that the supplied `applicantId` exists in the user's cached organization data (`ORGINFO` segment):
- Searches the `organizations[]` array for any organization whose `id` matches the supplied `applicantId` (case-insensitive)
- Rejects `Guid.Empty` early without a cache lookup

### `ValidateContactOwnershipAsync(contactId, profileContext)`

Used on **edit, delete, set-primary** operations. Searches the cached `contacts[]` array for a contact with the matching `contactId`. Returns both ownership status and `isEditable` flag.

### `ValidateAddressOwnershipAsync(addressId, profileContext)`

Used on **edit, delete, set-primary** operations. Searches the cached `addresses[]` array for an address with a matching `id`. Returns both ownership status and `isEditable` flag.

### `ValidateOrganizationOwnershipAsync(organizationId, profileContext)`

Used on **edit** operations. Searches the cached `organizations[]` array for an organization whose `id` matches the supplied `organizationId` (case-insensitive).

---

## Cache Segments

The validator reads from these cache segments (scoped to `{profileId}:{pluginId}:{provider}`):

| Segment | Used By | Data Structure |
|---------|---------|---------------|
| `{provider}:CONTACTINFO` | Contact validation | `{ applicantId, contacts: [{ contactId, isEditable, applicantId }] }` |
| `{provider}:ADDRESSINFO` | Address validation | `{ addresses: [{ id, isEditable }] }` |
| `{provider}:ORGINFO` | Organization + Applicant validation | `{ organizations: [{ id }] }` |

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

## Open Design Question: Submission/Application Write Endpoints

There is currently **no write (create/edit/delete) endpoint for submissions or applications** — only the read-side check described in the note at the top of this document. Before one is built, an explicit decision is needed on how it should treat Unity's applicant-merging behavior:

- **Option A** — trust Unity's merged list the same way the read path does today (a user authenticated under one sub of a merged applicant could update a submission created under a sibling sub).
- **Option B** — add a Portal-side guard that only allows writes to submissions fetched under the *exact* `Subject` used to authenticate the current request, rejecting merged-in siblings even though they're readable.

This is a product/security decision, not just an implementation detail — confirm with the Unity team what guarantees `SUBMISSIONINFO`/any future write endpoint actually provides before choosing.

---

## Related Documentation

- [API Endpoints](../auto/API-Endpoints.md) — Endpoint reference with authorization details
- [Plugin Architecture](Plugin-Architecture.md) — Plugin system design and request flow
- [Messaging Plugin Integration Guide](Messaging-Plugin-Integration-Guide.md) — Outbox/inbox messaging pattern
