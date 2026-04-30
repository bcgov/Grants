---
name: env-check
description: Audit environment variable consistency across environment.ts, environment.deploy.ts, and server.js — flags missing vars, hardcoded deploy values, and missing process.env fallbacks.
---

Audit environment variable consistency across all configuration files in the Grants Applicant Portal frontend.

Variable name to check (optional — omit to audit all): $ARGUMENTS

## Steps

1. **Read all three configuration surfaces** in parallel:
   - `src/environments/environment.ts`
   - `src/environments/environment.deploy.ts`
   - `server.js` (look for `process.env.*` references and the env-substitution block)

2. **Build a cross-reference table** — for every environment variable found across any of the three files, show its presence/absence and value in each:

   | Variable | environment.ts | environment.deploy.ts | server.js |
   | --- | --- | --- | --- |
   | `keycloakUrl` | ✅ hardcoded | ✅ from env | ✅ `KEYCLOAK__AUTHSERVERURL` |
   | `someVar` | ✅ | ❌ missing | ❌ missing |

3. **Flag inconsistencies**:
   - Variable present in `environment.ts` but missing from `environment.deploy.ts` (deploy will use wrong/undefined value)
   - Variable referenced in `server.js` but no corresponding key in either environment file (Angular app won't receive the value)
   - Hardcoded values in `environment.deploy.ts` that should come from `process.env` (breaks container configuration)
   - `process.env` references in `server.js` with no fallback (will be `undefined` in dev)

4. **If a specific variable was provided** (`$ARGUMENTS` is not empty), narrow the audit to just that variable and trace exactly how it flows from container env → `server.js` → `environment.deploy.ts` → Angular component.

5. **Report findings** — list issues by severity (missing in deploy config is high; missing fallback is medium), and suggest the exact line to add for each fix.

## Rules
- Do not modify any files — this is a read-only audit
- Check `angular.json` `fileReplacements` to confirm `environment.deploy.ts` is actually substituted at build time
- If `$ARGUMENTS` is empty, audit everything; do not ask for clarification, just run the full audit
