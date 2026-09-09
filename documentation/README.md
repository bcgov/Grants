# Documentation

**New to the project?** Start with the handover briefing [Applicant Portal, from zero](handover/Applicant-Portal-Orientation.html) — it covers what the system is, one request traced end to end, the conventions, local setup, and where to go next. Then read [Redis in the Unity bridge](handover/Redis-Unity-Bridge.html).

## Folder structure

| Folder | Who maintains it | When to update |
|---|---|---|
| [`auto/`](auto/) | **Auto-documenter agent** | Updated automatically after every ticket, bug fix, or refactor that changes the relevant code |
| [`architecture/`](architecture/) | Humans (agent patches stale sections) | When an architectural pattern is introduced or significantly changed |
| [`guides/`](guides/) | Humans only | When setup steps or how-to procedures change |
| [`integration-specs/`](integration-specs/) | Humans only — **external contracts** | Only with explicit versioning intent; changes affect external consumers |
| [`architecture-decisions/`](architecture-decisions/) | Humans only — **immutable records** | New ADR per decision; existing ADRs are never edited |
| [`handover/`](handover/) | Humans only | Presentation/briefing docs for team knowledge transfer; updated as understanding evolves |

## Index

### Handover briefings — read these first

| Doc | Covers |
|---|---|
| [Applicant Portal, from zero](handover/Applicant-Portal-Orientation.html) | Day-one orientation: system boundaries, repo map, request lifecycle, conventions, setup, tests, process, glossary, gotchas |
| [Redis in the Unity bridge](handover/Redis-Unity-Bridge.html) | Cache-aside reads, optimistic writes, outbox/inbox, ack/nack at both layers, cache compensation |

### Architecture

| Doc | Covers |
|---|---|
| [Plugin Architecture](architecture/Plugin-Architecture.md) | The plugin abstraction, registry, request flow, and how to add a plugin |
| [Authentication](architecture/Authentication.md) | Keycloak OIDC, JWT validation, claims, and authorization policies |
| [API Access Patterns](architecture/API-Access-Patterns.md) | SPA versus direct/service-account API access, and the authorization model |
| [Frontend Integration](architecture/Frontend-Integration.md) | Auth flow, role mapping, CORS, and error handling for frontend clients |
| [Unity Integration](architecture/Unity-Integration.md) | The Unity Grant Manager contract — reads, writes, messaging, applicant merging |
| [Messaging Plugin Integration Guide](architecture/Messaging-Plugin-Integration-Guide.md) | Outbox/inbox internals, plugin-agnostic messaging design, configuration, monitoring |
| [Resource Ownership Validation](architecture/Resource-Ownership-Validation.md) | How the API proves a caller owns the resource they asked for |
| [Client-Side Submission PDF Generation](architecture/Client-Side-Submission-PDF-Generation.md) | How the submission PDF is rendered in the browser |

### Guides

| Doc | Covers |
|---|---|
| [Secrets Management](guides/Secrets-Management.md) | User secrets locally, environment variables in deployment — required before first run |
| [Getting Keycloak Tokens](guides/Getting-Keycloak-Tokens.md) | Obtaining a personal IDIR/BCeID JWT for API testing |
| [Adding Authorization Policies](guides/Adding-Authorization-Policies.md) | Worked examples of new policy types |
| [Messaging Testing Guide](guides/Messaging-Testing-Guide.md) | End-to-end manual verification of the messaging flow |
| [SonarCloud Guide](guides/unity-grants-sonarcloud-readme.md) | Code quality setup, IDE integration, CI and PR analysis |

### Reference

| Doc | Covers |
|---|---|
| [API Endpoints](auto/API-Endpoints.md) | Generated reference for every REST endpoint — auto-maintained |
| [OpenShift Environment Configuration](OPENSHIFT_ENVIRONMENT_CONFIG.md) | ArgoCD environment variables, secrets, routes, and health probes |
| [ADR index](architecture-decisions/README.md) | Architecture decision records and the ADR process |

### Integration specs — external contracts

| Doc | Covers |
|---|---|
| [UNITY RabbitMQ Integration Spec](integration-specs/UNITY-RabbitMQ-Integration-Spec.md) | Topology, message shapes, and acknowledgment rules UNITY must implement |
| [UNITY Submission Form Integration Spec](integration-specs/UNITY-SubmissionForm-Integration-Spec.md) | The PDF-source-data endpoint contract |
| [Keycloak Placeholders](integration-specs/KEYCLOAK-PLACEHOLDERS.md) | Standard placeholder values used across docs and samples |

## Adding new documentation

- **New API endpoint** → `auto/API-Endpoints.md` is updated automatically by the auto-documenter agent.
- **New architectural pattern** → add a file to `architecture/` describing the pattern, decisions, and examples.
- **New setup or how-to guide** → add a file to `guides/`.
- **New external integration contract** → add a versioned file to `integration-specs/` and communicate the change to consumers.
- **New architectural decision** → follow the ADR format in `architecture-decisions/README.md`.
- **New team briefing / knowledge-transfer doc** → add a file to `handover/`.

When you add a doc, add it to the index above as well — otherwise nobody finds it.
