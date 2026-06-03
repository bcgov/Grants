# Documentation

## Folder structure

| Folder | Who maintains it | When to update |
|---|---|---|
| [`auto/`](auto/) | **Auto-documenter agent** | Updated automatically after every ticket, bug fix, or refactor that changes the relevant code |
| [`architecture/`](architecture/) | Humans (agent patches stale sections) | When an architectural pattern is introduced or significantly changed |
| [`guides/`](guides/) | Humans only | When setup steps or how-to procedures change |
| [`integration-specs/`](integration-specs/) | Humans only — **external contracts** | Only with explicit versioning intent; changes affect external consumers |
| [`architecture-decisions/`](architecture-decisions/) | Humans only — **immutable records** | New ADR per decision; existing ADRs are never edited |

## Adding new documentation

- **New API endpoint** → `auto/API-Endpoints.md` is updated automatically by the auto-documenter agent.
- **New architectural pattern** → add a file to `architecture/` describing the pattern, decisions, and examples.
- **New setup or how-to guide** → add a file to `guides/`.
- **New external integration contract** → add a versioned file to `integration-specs/` and communicate the change to consumers.
- **New architectural decision** → follow the ADR format in `architecture-decisions/README.md`.
