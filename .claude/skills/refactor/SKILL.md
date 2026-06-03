---
name: refactor
description: Refactor a component, service, or domain area in the Grants Applicant Portal — understands existing code, plans changes, implements via specialist sub-agents, and verifies nothing broke.
---

Refactor code in the Grants Applicant Portal.

Target and goal: $ARGUMENTS

Examples:
- `refactor applications/Grants.ApplicantPortal/src/Grants.ApplicantPortal.Frontend/src/app/features/applicant-info/ extract shared address form logic into a shared service`
- `refactor API.UseCases/Addresses reduce duplication between Create and Update handlers`

If `$ARGUMENTS` is empty, ask the user for the target (file, folder, or domain) and the refactoring goal before proceeding.

---

## Phase 1 — Understand current code

Use the **Explore** sub-agent to deeply read the target code:

- What does it currently do?
- What are the pain points or duplications?
- What are the external callers / dependents that must not break?
- What tests currently cover this code?

Produce a summary: "current state" in 5–10 bullet points.

---

## Phase 2 — Plan

Design the refactoring:

- List each change as: `<file> — <what changes and why>`
- Confirm the change is purely structural — **no new behaviour** unless the user explicitly asked for it
- Confirm all existing callers / tests will still work after the change
- Estimate risk: low (rename/extract) / medium (interface change) / high (architectural shift)

Present the plan to the user. Wait for approval before implementing.

---

## Phase 3 — Implement

Delegate to the appropriate specialist:

**Backend refactor** → **backend-developer** sub-agent
**Frontend refactor** → **frontend-developer** sub-agent
**Both** → both in parallel

Instruct the sub-agent:
- Follow the approved plan exactly — no scope creep
- Preserve all existing public interfaces unless the plan explicitly changes them
- Read every file before editing it

---

## Phase 4 — Verify

Delegate to the **test-guardian** sub-agent:

- Run the full test suite for the affected layer
- If tests fail: the refactor broke something — fix it before proceeding
- If coverage dropped: ask the user if new tests should be added

---

## Phase 5 — Review

Delegate to the **code-reviewer** sub-agent to confirm the refactored code still follows all project conventions.

---

## Phase 6 — AutoUI Guard

Delegate to the **autoui-guardian** sub-agent.

Pass it:
- A description of what changed in the UI (if anything): routes, selectors, element structure, text, navigation
- The list of files modified

The agent will check whether any existing Cypress specs or page objects in `applications/Grants.AutoUI/` are affected, and update them if needed.

**Skip this phase and state "no AutoUI changes required" if the refactor was backend-only or had no UI-visible impact.**

---

## Phase 7 — Document

Delegate to the **auto-documenter** sub-agent.

Pass it:
- Whether any API routes, request/response shapes, or auth policies changed as part of the refactor
- Whether any architectural patterns were renamed or restructured in a way that would make `documentation/architecture/` docs inaccurate
- The list of files modified

**Skip this phase and state "no documentation changes required" if the refactor was purely internal with no externally visible changes.**

---

## Phase 8 — Summary

```
## Refactor complete

### What changed
<bullet list>

### Files modified
<list>

### What stayed the same
<confirm public interfaces / callers not broken>

### Test results
<suite: X passed>

### AutoUI
<list of Cypress specs/page objects fixed — or "no AutoUI changes required">

### Documentation
<docs updated — or "no documentation changes required">
```

## Rules

- A refactor must not change observable behaviour — if behaviour needs to change, that is a feature, not a refactor
- Never approve a refactor that causes test failures
- If the plan changes during implementation, surface it to the user before continuing
