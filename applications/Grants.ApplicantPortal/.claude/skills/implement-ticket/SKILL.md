---
name: implement-ticket
description: End-to-end ticket implementation for the Grants Applicant Portal — parses ticket details, architects a solution across frontend and/or backend, implements using specialist sub-agents in parallel, runs tests, and produces a PR-ready summary.
---

Implement a development ticket end-to-end for the Grants Applicant Portal.

Ticket details: $ARGUMENTS

If `$ARGUMENTS` is empty, ask the user to paste the ticket details (title, description, acceptance criteria) before proceeding.

---

## Phase 1 — Analyse

Parse the ticket and determine:

1. **Scope**: backend only / frontend only / full-stack
2. **Domain**: which domain area is affected (Addresses, Contacts, Submissions, Payments, etc.)
3. **Type**: new feature / bug fix / enhancement / refactor
4. **Acceptance criteria**: list each testable criterion explicitly
5. **Open questions**: anything ambiguous that would block implementation — ask the user now before writing any code

Do not proceed past Phase 1 until all open questions are resolved.

---

## Phase 2 — Explore

Use the **Explore** sub-agent to read the codebase and answer:

- What existing files are affected or referenced?
- What patterns should be followed (read an analogous feature)?
- What domain models, interfaces, or services already exist that this ticket should use?

Produce a short findings summary before moving to Phase 3.

---

## Phase 3 — Architect

Design the solution:

- List every file to be **created** with its purpose
- List every file to be **modified** with what changes
- For full-stack tickets: separate the backend plan from the frontend plan
- Confirm the plan covers all acceptance criteria from Phase 1

Present the plan to the user. Wait for approval (or adjustments) before implementing.

---

## Phase 3.5 — Compliance gate (mandatory — do not skip)

Before writing a single line of code, check the approved plan against the project guides.

**Read the relevant guide(s):**
- Frontend work → read `src/Grants.ApplicantPortal.Frontend/.claude/UI_STYLE_GUIDE.md`
- Backend work → read `src/Grants.ApplicantPortal.Backend/.claude/ARCHITECTURE_GUIDE.md`
- Full-stack → read both

**Check every planned file and change against the "What requires a deviation confirmation" section of each guide.**

If violations are found, present them clearly:

```
⚠️  Style/Architecture deviations detected in the proposed plan:

FRONTEND
  - [file] [specific deviation — e.g. "uses *ngIf instead of @if"]
  - [file] [specific deviation]

BACKEND
  - [file] [specific deviation — e.g. "business logic in HandleAsync, not in a handler"]

These deviate from the project style/architecture guides.

Options:
  1. Revise the plan to comply (recommended)
  2. Proceed anyway — type "proceed with deviations" to confirm

Which would you like to do?
```

**Do not proceed to Phase 4 until the user either:**
- Approves a revised compliant plan, OR
- Explicitly types **"proceed with deviations"**

If the plan is fully compliant, state: `✅ Plan checked against style/architecture guides — no deviations found.` and continue automatically.

---

## Phase 4 — Implement

Execute based on scope:

**Backend work** → delegate to the **backend-developer** sub-agent
**Frontend work** → delegate to the **frontend-developer** sub-agent
**Full-stack** → delegate both in parallel, they work independently

Pass each sub-agent:
- The relevant part of the architectural plan from Phase 3
- The acceptance criteria their work must satisfy
- A reminder to read existing parallel files before writing

Wait for both to complete before proceeding.

---

## Phase 5 — Test

Delegate to the **test-guardian** sub-agent:

- Run the relevant test suite(s) for the changed code
- If tests fail: fix them (or ask the appropriate developer sub-agent to fix them), then re-run
- Confirm all tests pass before proceeding

---

## Phase 6 — Review

Delegate to **code-reviewer** and **security-reviewer** sub-agents in parallel:

- If either finds violations: fix them via the appropriate developer sub-agent, then re-review
- Continue until both reviewers report clean

---

## Phase 7 — Summary

Produce a commit-ready summary:

```
## What was implemented
<bullet list of changes>

## Files changed
<file list with brief description of each change>

## Acceptance criteria coverage
<each criterion from Phase 1 and how it was met>

## Test results
<suite name: X passed, 0 failed>
```

## Rules

- Never skip Phase 1 open questions — ambiguity costs more to fix later than to clarify now
- Never start implementation before the Phase 3 plan is approved
- Never mark complete if tests are failing
- If scope creep is detected during implementation, pause and surface it to the user
