---
name: review-pr
description: Full PR review for the Grants Applicant Portal — runs security, architecture/conventions, and test coverage reviews in parallel and produces a structured report ready to paste into GitHub.
---

Review a pull request for the Grants Applicant Portal.

PR reference: $ARGUMENTS

If `$ARGUMENTS` is empty, review the current branch diff against `main`.

---

## Phase 1 — Get the diff

Collect the changes to review:

```bash
git diff main...HEAD --name-only     # files changed
git diff main...HEAD                 # full diff
git log main...HEAD --oneline        # commits on this branch
```

Summarise the scope in one paragraph: what domains are touched, frontend vs backend vs both, rough size of change.

---

## Phase 2 — Parallel review

Spawn all three reviewers simultaneously:

**→ security-reviewer sub-agent**
Provide: the full diff and list of changed files
Ask for: Critical / High / Informational findings

**→ code-reviewer sub-agent**
Provide: the full diff and list of changed files
Ask for: rule violations and suggestions

**→ test-guardian sub-agent**
Provide: the list of changed files
Ask for: run the relevant test suites and report pass/fail; flag any changed logic that has no test coverage

Wait for all three to complete.

---

## Phase 3 — Synthesise

Combine the three reports into a single structured review:

```markdown
## PR Review — <branch name>

### Scope
<one paragraph from Phase 1>

### Security
<security-reviewer findings, or "No issues found">

### Architecture & Conventions
<code-reviewer findings, or "No violations found">

### Tests
<test-guardian results: X passed, Y failed; coverage gaps if any>

### Summary
**Verdict**: APPROVE / REQUEST CHANGES / NEEDS DISCUSSION

**Must fix before merge** (if any):
- <item>

**Suggestions** (non-blocking):
- <item>
```

---

## Rules

- Do not invent findings — only report what the sub-agents found
- A clean review is a valid result — say so clearly
- "Request changes" requires at least one Critical or High finding
- Do not approve if any test suite is failing
