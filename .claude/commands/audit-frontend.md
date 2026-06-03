# Run a security audit of the Grants Applicant Portal frontend, apply safe fixes, and verify nothing broke

Frontend root: `applications/Grants.ApplicantPortal/src/Grants.ApplicantPortal.Frontend/`

---

## Step 1 — Audit

Run from the frontend root:

```bash
npm audit
```

Parse the output and report:

```
## Vulnerability summary

| Severity | Count |
|---|---|
| Critical | <n> |
| High      | <n> |
| Moderate  | <n> |
| Low       | <n> |

### Details
<list each vulnerability: package name, severity, description, fix available?>
```

If there are **zero vulnerabilities**, state that and stop — do not run further steps.

---

## Step 2 — Apply safe fixes

Run:

```bash
npm audit fix
```

Report every package that was updated (name, old version → new version). If `npm audit fix` cannot resolve some vulnerabilities without breaking changes, list them clearly:

```
## Not fixed (require --force or manual intervention)
<package — reason — recommended action>
```

Do **not** run `npm audit fix --force` without explicit user approval — it may introduce breaking changes.

---

## Step 3 — Verify

Run the full Karma/Jasmine test suite:

```bash
npm test -- --no-progress --watch=false --browsers=ChromeHeadless
```

Report the result:

```
## Test results
<X passed, Y failed, Z skipped>
```

If any tests fail:
1. Show the failing test names and error messages
2. Determine whether the failure is caused by the package updates from Step 2
3. If yes — revert the offending update with `npm install <package>@<previous-version>` and re-run tests
4. If no — report the pre-existing failure separately so it is not conflated with the audit

---

## Final summary

```
## Audit complete

### Vulnerabilities resolved
<list of fixed packages and versions>

### Vulnerabilities remaining (manual action needed)
<list or "none">

### Test result
<passed / failed — details if failed>

### Recommended next steps
<any remaining vulnerabilities that need manual review or --force>
```
