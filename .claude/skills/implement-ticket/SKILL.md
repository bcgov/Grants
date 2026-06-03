---
name: implement-ticket
description: End-to-end ticket implementation for the Grants Applicant Portal — parses ticket details, architects a solution across frontend and/or backend, implements using specialist sub-agents in parallel, runs tests, and produces a PR-ready summary.
---

Implement a development ticket end-to-end for the Grants Applicant Portal.

Ticket details: $ARGUMENTS

If `$ARGUMENTS` is empty, ask the user to paste the ticket details (title, description, acceptance criteria) before proceeding.

**Ticket number**: Extract `AB#<number>` from the ticket details. If no ticket number is present, ask: *"What is the AB ticket number? (e.g. AB#12345)"* — do not proceed until you have it.

---

## Phase 1 — Analyse

Parse the ticket and determine:

1. **Ticket number**: confirm the `AB#<number>` extracted above
2. **Scope**: backend only / frontend only / full-stack
3. **Domain**: which domain area is affected (Addresses, Contacts, Submissions, Payments, etc.)
4. **Type**: new feature / bug fix / enhancement / refactor
5. **Acceptance criteria**: list each testable criterion explicitly
6. **Open questions**: anything ambiguous that would block implementation — ask the user now before writing any code

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
- Frontend work → read `applications/Grants.ApplicantPortal/src/Grants.ApplicantPortal.Frontend/.claude/UI_STYLE_GUIDE.md`
- Backend work → read `applications/Grants.ApplicantPortal/src/Grants.ApplicantPortal.Backend/.claude/ARCHITECTURE_GUIDE.md`
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
- An explicit instruction: **unit tests are part of this phase** — every new handler (backend) and every new component/service (frontend) must have a spec file written before the sub-agent reports done

Wait for both to complete before proceeding.

---

## Phase 5 — Test

Delegate to the **test-guardian** sub-agent:

- Run the full relevant test suite(s) — tests were written in Phase 4, this is the verification gate
- If any tests fail: diagnose the root cause, fix via the appropriate developer sub-agent, then re-run
- If any new code is found to be untested (Phase 4 missed it): write the missing tests now
- Confirm all tests pass before proceeding

---

## Phase 6 — Review

Delegate to **code-reviewer** and **security-reviewer** sub-agents in parallel:

- If either finds violations: fix them via the appropriate developer sub-agent, then re-review
- Continue until both reviewers report clean

---

## Phase 7 — AutoUI Guard

Delegate to the **autoui-guardian** sub-agent.

Pass it:
- A description of all UI/frontend changes made: routes added or changed, form fields added or renamed, element selectors changed, navigation changes, text or label changes
- A description of any new features, pages, or user flows introduced
- The full list of files modified in this ticket

The agent will:
1. Check whether any existing Cypress specs or page objects in `applications/Grants.AutoUI/` are broken by the changes, and fix them (self-healing)
2. Create stub spec files (with `it.skip` placeholders) for any new user-facing feature that warrants future automated testing

**Skip this phase and state "no AutoUI changes required" if the ticket is backend-only with no UI changes.**

---

## Phase 8 — Document

Delegate to the **auto-documenter** sub-agent.

Pass it:
- A description of all API changes: endpoints added, modified, or removed
- A description of any new architectural patterns introduced (new domain, new auth mechanism, new integration)
- The list of files modified in this ticket

The agent will:
1. Update `documentation/auto/API-Endpoints.md` if any endpoints changed
2. Patch stale sections in `documentation/architecture/` if a described pattern changed
3. Create an architecture doc stub for any genuinely new pattern or domain

**Skip this phase and state "no documentation changes required" if the ticket was a pure frontend feature with no backend API changes and no new patterns.**

---

## Phase 9 — Branch & Commit

Create the branch and commit all changes made during this ticket.

**Step 1 — Confirm branch type.**
Derive from the ticket type determined in Phase 1:
- New feature or enhancement → `feature/AB#<ticket>-<slug>`
- Bug fix → `bugfix/AB#<ticket>-<slug>`
- Hotfix (only if the user explicitly requested it) → `hotfix/AB#<ticket>-<slug>`

If the type is ambiguous, ask: *"Should this branch be `feature` or `bugfix`?"*

**Step 2 — Confirm slug.**
Derive a 2–4 word kebab-case slug from the ticket title (e.g. `redis-exceptions`, `address-validation`, `null-reference-fix`).
Present it and ask: *"I'll name the branch `<full-branch-name>` — does that slug work, or would you prefer something different?"*
Wait for confirmation before continuing.

**Step 3 — Confirm base branch.**
- `feature` and `bugfix` → base is always `dev`
- `hotfix` → ask: *"Which base branch for the hotfix — `test` or `main`?"*

**Step 4 — Execute.**

```bash
git checkout <base-branch>
git checkout -b <branch-type>/AB#<ticket>-<slug>
```

Stage every file created or modified during this implementation (be explicit — list files individually, do not use `git add .`) and commit:

```bash
git add <file1> <file2> ...
git commit -m "AB#<ticket> <short description>"
```

Report the branch name and commit hash on completion.

---

## Phase 10 — Summary

```
## Branch & commit
<branch name created>
<commit hash and message>

## What was implemented
<bullet list of changes>

## Files changed
<file list with brief description of each change>

## Acceptance criteria coverage
<each criterion from Phase 1 and how it was met>

## Test results
<suite name: X passed, 0 failed>

## AutoUI
<list of Cypress specs/page objects fixed, or stubs created — or "no AutoUI changes required">

## Documentation
<list of docs updated or stubs created — or "no documentation changes required">
```

## Rules

- Never skip Phase 1 open questions — ambiguity costs more to fix later than to clarify now
- Never start implementation before the Phase 3 plan is approved
- Never mark complete if tests are failing
- If scope creep is detected during implementation, pause and surface it to the user
