# Cypress GitHub Workflows

Headless Cypress E2E testing for the Grants Applicant Portal, run in GitHub Actions against live deployed environments.

---

## Design

### Workflow files

| File | Purpose |
|---|---|
| `.github/workflows/cypress-e2e-runner.yml` | Shared runner — called by all other workflows. Writes the env config, installs deps, runs Cypress, uploads screenshots on failure. |
| `.github/workflows/cypress-dev.yml` | Auto-triggers after a successful dev deployment. Manual dispatch also available. |
| `.github/workflows/cypress-test.yml` | Auto-triggers after a successful test deployment. Manual dispatch also available. |
| `.github/workflows/cypress-uat.yml` | Auto-triggers after a successful main deployment. Manual dispatch also available. |
| `.github/workflows/cypress-prod.yml` | Manual-only. Run after UAT sign-off — never automatically after a deployment. |
| `.github/workflows/cypress-on-demand.yml` | Manual-only. For spec development on feature/bugfix branches. |

### Automation chain

```
push to dev   →  Dev - Build & Push docker images   →  Cypress E2E — Dev
push to test  →  Test - Build & Push docker images  →  Cypress E2E — Test
push to main  →  Main - Build & Push docker images  →  Cypress E2E — UAT
                                                    (Prod is triggered manually after UAT sign-off)
```

Each Cypress workflow only proceeds if the upstream build/push workflow **succeeded**. A failed deployment does not trigger a test run.

### Environment config at runtime

The `cypress/config/` files (`dev.json`, `test.json`, `uat.json`, `prod.json`) are in `.gitignore` and are never committed. In CI the runner extracts the env's entry from the `GRANTS_CYPRESS_CONFIG` secret and writes it to `cypress/config/<env>.json` immediately before `npm ci` runs.

`cypress.config.ts` reads the file at startup and merges it into the Cypress environment. Runtime `--env` flags always override file values, which is how the optional `base_url` override works in `cypress-dev.yml`.

### Runner inputs

`cypress-e2e-runner.yml` accepts these inputs from its callers (who also pass `secrets: inherit`):

| Input | Required | Description |
|---|---|---|
| `env_name` | Yes | One of `dev`, `test`, `uat`, `prod` — selects the config file and names the job. |
| `cypress_config_key` | Yes | Key in `GRANTS_CYPRESS_CONFIG` for this env (`CYPRESS_CONFIG_DEV`, `_TEST`, `_UAT`, `_PROD`). |
| `base_url` | No | Overrides the `baseUrl` from the config file. Used by `cypress-dev.yml` when pointing at a non-default URL. |

---

## Configuration

### Step 1 — Create the GitHub Secret

One repository secret, `GRANTS_CYPRESS_CONFIG`, under **Settings → Secrets and variables → Actions**. It is a JSON object with one key per environment; each value is the **full JSON** for that environment's config file (as a nested object or a JSON string):

```json
{
  "CYPRESS_CONFIG_DEV":  { "baseUrl": "https://dev-grants.apps.silver.devops.gov.bc.ca/", "environment": "dev", "...": "..." },
  "CYPRESS_CONFIG_TEST": { "...": "..." },
  "CYPRESS_CONFIG_UAT":  { "...": "..." },
  "CYPRESS_CONFIG_PROD": { "...": "..." }
}
```

This is the same shape Unity uses for `UNITY_CYPRESS_CONFIG`. A full template with every key and base URL is in [`cypress/config/grants-cypress-config.json.example`](cypress/config/grants-cypress-config.json.example) — copy it, fill in the credentials, and paste the result as the secret value. GitHub Actions and Azure runners use the same combined value.

### Step 2 — Config file format

Each environment's value is a flat JSON object. Use `cypress/config/grants-cypress-config.json.example` as the field reference — it shows all keys with empty placeholder values. Fill in the real values for each environment:

```json
{
  "baseUrl": "https://dev-grants.apps.silver.devops.gov.bc.ca/",
  "environment": "dev",
  "test1username": "",
  "test1password": "",
  "test2username": "",
  "test2password": "",
  "bcscUsername": "",
  "bcscPassword": "",
  "workspaceName": "",
  "providerName": ""
}
```

Base URLs per environment:

| Environment | Base URL |
|---|---|
| dev | `https://dev-grants.apps.silver.devops.gov.bc.ca/` |
| test | `https://test-grants.apps.silver.devops.gov.bc.ca/` |
| uat | `https://uat-grants.apps.silver.devops.gov.bc.ca/` |
| prod | `https://grants.apps.silver.devops.gov.bc.ca/` |

### Step 3 — Verify the trigger workflow names match

The `workflow_run:` triggers reference the upstream deploy workflow names exactly. Confirm these match the `name:` field at the top of each deploy workflow file:

| Cypress workflow | Expects upstream name |
|---|---|
| `cypress-dev.yml` | `Dev - Build & Push docker images` |
| `cypress-test.yml` | `Test - Build & Push docker images` |
| `cypress-uat.yml` | `Main - Build & Push docker images` |

---

## Automation — what runs automatically

### Dev

Triggered when `Dev - Build & Push docker images` completes successfully on the `dev` branch.

**Actions → Cypress E2E — Dev** will show the run with the job name `Cypress E2E — dev`.

### Test

Triggered when `Test - Build & Push docker images` completes successfully on the `test` branch.

### UAT

Triggered when `Main - Build & Push docker images` completes successfully on the `main` branch.

### Prod

Not automated. See the prod sign-off flow below.

---

## Developer Flows

### Flow 1 — Run tests manually against a live environment

Use this to re-run tests after an investigation, or when an automated run was skipped.

1. Go to **Actions** in GitHub.
2. Select **Cypress E2E — Dev**, **Cypress E2E — Test**, or **Cypress E2E — UAT** from the left panel.
3. Click **Run workflow**.
4. Leave the branch as `main` (the workflow code lives there; the target environment is fixed by the workflow).
5. Click **Run workflow**.

### Flow 2 — Develop and test a new Cypress spec from a feature branch

Use this when writing or debugging specs on a `feature/` or `bugfix/` branch. Your branch's spec files are checked out and run against the real dev or test application and database — no local setup needed.

1. Push your in-progress spec to your feature or bugfix branch.
2. Go to **Actions → Cypress E2E — On Demand**.
3. Click **Run workflow**.
4. In the **Branch** dropdown, select your feature or bugfix branch (e.g. `feature/AB#1234-my-spec`).
5. In the **Target environment** dropdown, select `dev` or `test`.
6. Click **Run workflow**.

Your specs run against the live environment. Credentials and base URL come from the `CYPRESS_CONFIG_DEV` or `CYPRESS_CONFIG_TEST` key of `GRANTS_CYPRESS_CONFIG` — no additional configuration required.

Screenshots of any failing tests are uploaded as a workflow artifact and available for download from the workflow run summary page.

### Flow 3 — Prod sign-off after UAT approval

Prod is never triggered automatically. Run it only after UAT has been signed off.

1. Go to **Actions → Cypress E2E — Prod**.
2. Click **Run workflow**.
3. Leave the branch as `main`.
4. Click **Run workflow**.

### Flow 4 — Dev run against a specific URL (advanced)

`cypress-dev.yml` accepts an optional `base_url` input that overrides the URL in the `CYPRESS_CONFIG_DEV` config. Use this if you need to point the dev test suite at a non-standard URL.

1. Go to **Actions → Cypress E2E — Dev**.
2. Click **Run workflow**.
3. Enter the target URL in the **Override baseUrl** field (e.g. `https://dev2-grants.apps.silver.devops.gov.bc.ca/`).
4. Click **Run workflow**.

Leave the field blank to use the default dev URL from the secret.

---

## Artifacts

On test failure, screenshots are automatically uploaded and attached to the workflow run. To access them:

1. Open the failed workflow run in **Actions**.
2. Scroll to the **Artifacts** section at the bottom of the run summary.
3. Download `cypress-screenshots-<env>-<run-number>.zip`.

Videos are not recorded by default (disabled in Cypress 10+). Enable them in `cypress.config.ts` if needed — the `.gitignore` already excludes the `cypress/videos/` output folder.

---

## Local development reference

To run tests locally before pushing, create your own config file (not committed):

```bash
# Copy the per-env template and fill in real values
cp cypress/config/dev.json.example cypress/config/dev.json

npm run cy:open:dev    # interactive Cypress UI against dev
npm run cy:run:dev     # headless run against dev
npm run validate:selectors   # verify data-cy selectors match Angular templates
```
