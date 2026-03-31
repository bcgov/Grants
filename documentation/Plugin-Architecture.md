# Plugin Architecture

## Overview

The Grants Applicant Portal uses a **plugin-based architecture** to integrate with external grant management systems. Plugins encapsulate all system-specific logic — data retrieval, write operations, messaging, and configuration — behind a set of standard interfaces.

This design allows the portal to support multiple backend systems (e.g., Unity Grant Manager, a demo/test system) through the same API surface.

---

## Core Concepts

### Plugin

A plugin is a class that implements one or more plugin interfaces. Each plugin has a unique `PluginId` (e.g., `"UNITY"`, `"DEMO"`) and declares the features it supports.

### Provider

A provider represents a data source or program within a plugin. For UNITY, providers map to **tenants** (grant programs). For DEMO, providers are hardcoded test programs (`PROGRAM1`, `PROGRAM2`). Providers are fetched at runtime via the `/Plugins/{PluginId}/providers` endpoint.

### Features

Each plugin declares its supported features as a string list. The system and UI use this to determine which operations are available. Current features:

| Feature | Description |
|---------|-------------|
| `ProfilePopulation` | Can populate profile data (contacts, addresses, orgs, submissions, payments) |
| `ContactManagement` | Supports contact create/edit/delete/set-primary |
| `AddressManagement` | Supports address edit/set-primary |
| `OrganizationManagement` | Supports organization edit |

---

## Plugin Interfaces

All interfaces are defined in the `Grants.ApplicantPortal.API.Core.Plugins` namespace.

### `IProfilePlugin` (required)

Base interface every plugin must implement.

```
PluginId: string
CanHandle(metadata): bool
GetSupportedFeatures(): IReadOnlyList<string>
GetProvidersAsync(profileId, subject, ct): IReadOnlyList<ProviderInfo>
GetContactRoles(): IReadOnlyList<ContactRoleOption>
```

Handles:
- Plugin identification and feature discovery
- Provider retrieval (programs/tenants available to the user)
- Contact role option definitions
- Profile data population (contacts, addresses, organizations, submissions, payments)

### `IContactManagementPlugin`

```
CreateContactAsync(...)
EditContactAsync(...)
DeleteContactAsync(...)
SetPrimaryContactAsync(...)
```

### `IAddressManagementPlugin`

```
EditAddressAsync(...)
SetPrimaryAddressAsync(...)
```

### `IOrganizationManagementPlugin`

```
EditOrganizationAsync(...)
```

---

## Registered Plugins

### UNITY (`PluginId = "UNITY"`)

Production plugin that integrates with the Unity Grant Manager.

**Features:** `ProfilePopulation`, `ContactManagement`, `AddressManagement`, `OrganizationManagement`

**Providers:** Fetched dynamically from the Unity tenants API (`/api/app/applicant-profiles/tenants`). Results are cached per profile.

**Contact roles:**
| Key | Label |
|-----|-------|
| `General` | General |
| `Primary` | Primary Contact |
| `Financial` | Financial Officer |
| `SigningAuthority` | Additional Signing Authority |
| `Executive` | Executive |

**Read operations:** Call the Unity REST API via `IExternalServiceClient`, cache results using `IPluginCacheService`.

**Write operations:** Update local cache optimistically, then publish a `PluginDataMessage` command to RabbitMQ via the transactional outbox. See [Messaging Plugin Integration Guide](Messaging-Plugin-Integration-Guide.md) for details.

**Source files:**
- `Unity/UnityPlugin.cs` — Plugin entry point, provider retrieval
- `Unity/Unity.Contacts.cs` — Contact CRUD + messaging
- `Unity/Unity.Addresses.cs` — Address management + messaging
- `Unity/Unity.Organizations.cs` — Organization management + messaging
- `Unity/Unity.Profile.cs` — Profile population
- `Unity/Unity.CacheHelpers.cs` — Cache key/serialization helpers

### DEMO (`PluginId = "DEMO"`)

Test/demonstration plugin with hardcoded data. No external dependencies.

**Features:** `ProfilePopulation`, `ContactManagement`, `AddressManagement`, `OrganizationManagement`

**Providers:** Hardcoded: `PROGRAM1`, `PROGRAM2`

**Contact roles:**
| Key | Label |
|-----|-------|
| `General` | General |
| `Primary` | Primary Contact |
| `Billing` | Billing |
| `Technical` | Technical |

**Read operations:** Returns seeded demo data from the distributed cache. Data is automatically seeded on first access.

**Write operations:** Updates the local cache directly. No messaging (no external system to notify).

**Source files:**
- `Demo/DemoPlugin.cs` — Plugin entry point, seeding logic
- `Demo/Demo.Contacts.cs` — Contact CRUD (cache-only)
- `Demo/Demo.Addresses.cs` — Address management (cache-only)
- `Demo/Demo.Organizations.cs` — Organization management (cache-only)
- `Demo/Demo.Profile.cs` — Profile population
- `Demo/Data/` — Hardcoded seed data (contacts, addresses, orgs, submissions, payments)

---

## Plugin Registration and Discovery

### PluginRegistry

`PluginRegistry` is a static, thread-safe registry initialized at application startup. It scans all `IProfilePlugin` implementations from DI and caches their metadata.

**Key operations:**
- `Initialize(serviceProvider, configuration)` — Called once during startup
- `GetConfiguredPlugin(pluginId)` — Fast plugin lookup by ID
- `GetConfiguredPlugins(enabledOnly)` — List all (or only enabled) plugins
- `GetAllPluginIds()` — List all registered plugin IDs
- `IsInitialized` — Health check flag

### PluginConfiguration

Plugin behavior is configurable via `appsettings.json`:

```json
{
  "Plugins": {
    "UNITY": {
      "Enabled": true,
      "BaseUrl": "https://unity.example.com",
      "ApiKey": "..."
    },
    "DEMO": {
      "Enabled": true
    }
  }
}
```

### ProfilePluginFactory

`IProfilePluginFactory` resolves plugin instances from DI by `PluginId`. Used by endpoints and use cases that need a live plugin instance (e.g., to call `GetProvidersAsync` or `GetContactRoles`).

---

## Request Flow

### Read (e.g., GET `/Contacts/UNITY/DGP`)

```
Request → ProfileResolutionMiddleware (resolves ProfileId from JWT)
        → FastEndpoint (RetrieveContacts)
        → MediatR Query (RetrieveContactsQuery)
        → Use Case Handler
            → Check cache
            → If miss: call plugin.PopulateContactsAsync()
                → Plugin calls external API + caches result
            → Return cached data
```

### Write (e.g., POST `/Contacts/UNITY/DGP`)

```
Request → ProfileResolutionMiddleware
        → FastEndpoint (Create)
        → MediatR Command (CreateContactCommand)
        → Use Case Handler
            → Call plugin.CreateContactAsync()
                → Update local cache
                → Publish PluginDataMessage to outbox (UNITY only)
            → Return result
        → OutboxProcessorJob (background)
            → Publish to RabbitMQ
        → External system processes + acks
```

---

## Adding a New Plugin

1. Create a new class implementing `IProfilePlugin` (and optional management interfaces)
2. Define a unique `PluginId`
3. Implement `GetSupportedFeatures()`, `GetProvidersAsync()`, `GetContactRoles()`, `CanHandle()`
4. Implement data population methods for each data type
5. Implement write operations if supporting management features
6. Register the plugin in DI via `PluginServiceExtensions`
7. Add configuration in `appsettings.json` under `Plugins:{YourPluginId}`
8. If messaging is needed, implement `IPluginMessageHandler` for acknowledgment processing

---

## Related Documentation

- [API Endpoints](API-Endpoints.md) — Complete endpoint reference
- [Messaging Plugin Integration Guide](Messaging-Plugin-Integration-Guide.md) — Outbox/inbox messaging pattern
- [Unity Integration](Unity-Integration.md) — Unity-specific integration details
- [UNITY RabbitMQ Integration Spec](UNITY-RabbitMQ-Integration-Spec.md) — External system consumer contract
