# AutoUI Cypress Config — Local Credentials Setup

The `cypress/config/*.json` files are **tracked in git** with empty placeholder credentials.  
To prevent accidentally committing your real credentials, each developer must run the following command once per clone.

## One-time setup (run after cloning or pulling these files)

```bash
git update-index --skip-worktree \
  applications/Grants.AutoUI/cypress/config/dev.json \
  applications/Grants.AutoUI/cypress/config/dev2.json \
  applications/Grants.AutoUI/cypress/config/test.json \
  applications/Grants.AutoUI/cypress/config/uat.json \
  applications/Grants.AutoUI/cypress/config/prod.json
```

After running this, fill in your credentials in the relevant config file(s). Git will ignore your local changes to these files going forward.

## Why `--skip-worktree` instead of `.gitignore`?

| Approach | Files tracked in remote? | Ignores local edits? |
|---|---|---|
| `.gitignore` | Deletes from remote | yes |
| `--skip-worktree` | Keeps placeholders in remote | yes |

## To temporarily re-enable tracking (e.g. to update placeholders)

```bash
git update-index --no-skip-worktree applications/Grants.AutoUI/cypress/config/<file>.json
# make your changes to the placeholder values, then commit and push
```
