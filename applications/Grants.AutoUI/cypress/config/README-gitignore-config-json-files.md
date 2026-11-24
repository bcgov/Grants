# Cypress Environment Config Files

The `*.json` config files in this folder are **excluded from git** (via `.gitignore`) because they contain credentials.  
Each developer must create their own local copies from the `.example` files provided.

## Setup — pick one method

### Option A: one file for all environments

```powershell
Copy-Item cypress/config/grants-cypress-config.json.example cypress/config/grants-cypress-config.json
```

Fill in the `CYPRESS_CONFIG_DEV` / `_DEV2` / `_TEST` / `_UAT` / `_PROD` blocks you use. It has the same shape as the
`GRANTS_CYPRESS_CONFIG` CI secret (see below).

`cypress.config.ts` uses `cypress/config/<env>.json` if it exists, otherwise the `CYPRESS_CONFIG_<ENV>` block of
`grants-cypress-config.json`. A per-env file always wins, so delete old `<env>.json` files when switching to Option A.

### Option B: one file per environment

Copy the example file(s) for the environment(s) you need and fill in your credentials:

```bash
# Windows (PowerShell)
Copy-Item cypress/config/dev.json.example   cypress/config/dev.json
Copy-Item cypress/config/dev2.json.example  cypress/config/dev2.json
Copy-Item cypress/config/test.json.example  cypress/config/test.json
Copy-Item cypress/config/uat.json.example   cypress/config/uat.json
Copy-Item cypress/config/prod.json.example  cypress/config/prod.json

# macOS / Linux
cp cypress/config/dev.json.example  cypress/config/dev.json
cp cypress/config/test.json.example cypress/config/test.json
# etc.
```

Then open the file and fill in your credentials:

```json
{
    "baseUrl": "https://dev-grants.apps.silver.devops.gov.bc.ca/",
    "environment": "dev",
    "test1username": "your-test-username",
    "test1password": "your-test-password",
    "test2username": "your-second-test-username",
    "test2password": "your-second-test-password",
    "bcscUsername": "your-bcsc-username",
    "bcscPassword": "your-bcsc-password",
    "workspaceName": "your-workspace-name",
    "providerName": "your-provider-name"
}
```

## Available example files

| Example file | Creates | Environment URL |
|---|---|---|
| `dev.json.example` | `dev.json` | https://dev-grants.apps.silver.devops.gov.bc.ca/ |
| `dev2.json.example` | `dev2.json` | https://dev2-grants.apps.silver.devops.gov.bc.ca/ |
| `test.json.example` | `test.json` | https://test-grants.apps.silver.devops.gov.bc.ca/ |
| `uat.json.example` | `uat.json` | https://uat-grants.apps.silver.devops.gov.bc.ca/ |
| `prod.json.example` | `prod.json` | https://prod-grants.apps.silver.devops.gov.bc.ca/ |

## CI runners (GitHub Actions / Azure)

CI does not use the per-env files above. Runners read one combined secret, `GRANTS_CYPRESS_CONFIG`, keyed `CYPRESS_CONFIG_DEV` / `_TEST` / `_UAT` / `_PROD` (`_DEV2` is for local runs only), and write each entry out to `<env>.json` at run time.

`grants-cypress-config.json.example` is the template for that secret. Copy it, fill in the credentials, and paste the whole JSON as the secret value. **Never commit the filled-in copy.**

## Important

- **Never commit your `*.json` files** — every `<env>.json` and `grants-cypress-config.json` is gitignored to protect credentials.
- **Do commit changes to `*.json.example` files** if the config structure or base URLs change.
