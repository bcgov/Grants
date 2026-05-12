---
name: fix-bug
description: Diagnose and fix a bug in the Grants Applicant Portal — takes a bug report or stack trace, locates the root cause, implements a targeted fix, and verifies with tests.
---

Diagnose and fix a bug in the Grants Applicant Portal.

Bug details: $ARGUMENTS

If `$ARGUMENTS` is empty, ask the user to describe the bug (what happened, what was expected, any error messages or stack traces) before proceeding.

---

## Phase 1 — Understand the bug

Parse the report and identify:

1. **Symptom**: what the user sees
2. **Expected behaviour**: what should happen
3. **Stack trace / error**: paste verbatim if provided
4. **Scope**: backend / frontend / both
5. **Reproducibility**: always / intermittent / specific conditions

---

## Phase 2 — Locate

Use the **Explore** sub-agent to find:

- The files most likely responsible based on the stack trace or symptom
- Any recent changes in those files (check git log if relevant)
- The specific line(s) where the failure occurs or originates

Produce a short diagnosis before writing any fix:
```
Root cause: <one sentence>
Location: <file path(s) and line reference(s)>
Why it breaks: <explanation>
```

Present this to the user. Confirm it's the correct diagnosis before fixing.

---

## Phase 3 — Fix

Delegate to the appropriate specialist sub-agent:

**Backend bug** → **backend-developer** sub-agent
**Frontend bug** → **frontend-developer** sub-agent
**Both** → both in parallel

Instruct the sub-agent to:
- Make the **smallest targeted change** that fixes the root cause
- Not refactor surrounding code unless it is directly causing the bug
- Not change test assertions to hide a failure — fix the source

---

## Phase 4 — Verify

Delegate to the **test-guardian** sub-agent:

- Run the test suite(s) covering the affected code
- If there was no test covering this bug path: write one that would have caught it
- Confirm all tests pass

---

## Phase 5 — Security check (if auth/data related)

If the bug involved auth, ownership validation, or data access, delegate to **security-reviewer** to confirm the fix doesn't introduce a new vulnerability.

---

## Phase 6 — AutoUI Guard

Delegate to the **autoui-guardian** sub-agent.

Pass it:
- A description of what the fix changed in the UI (if anything): routes, selectors, form fields, text, navigation
- The list of files modified

The agent will check whether any existing Cypress specs or page objects in `applications/Grants.AutoUI/` are affected by the fix, and update them if needed.

**Skip this phase and state "no AutoUI changes required" if the bug was backend-only with no UI changes.**

---

## Phase 7 — Document

Delegate to the **auto-documenter** sub-agent.

Pass it:
- A description of any API changes the fix introduced (corrected route, changed request/response shape)
- The list of files modified

**Skip this phase and state "no documentation changes required" if the fix had no API or pattern changes.**

---

## Phase 8 — Summary

```
## Bug fixed
<one-line description>

## Root cause
<explanation>

## Fix
<what changed and why>

## Files modified
<list>

## Test added
<test name and what it covers — or "existing tests cover this">

## AutoUI
<list of Cypress specs/page objects fixed — or "no AutoUI changes required">

## Documentation
<docs updated — or "no documentation changes required">
```

## Rules

- Fix the root cause, not the symptom
- The smallest correct fix is always preferred over a large refactor
- Never mark complete if any test is failing
- If the bug reveals a design flaw that needs a broader fix, surface it to the user rather than patching around it
