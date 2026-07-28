# Grants.AutoUI

Cypress E2E suite. Targets deployed environments — tests do **not** run against localhost.

```bash
cd applications/Grants.AutoUI
npm run cy:open:dev    # interactive Cypress UI against dev
npm run cy:run:dev     # headless run against dev
npm run cy:run:test    # headless run against test
npm run validate:selectors   # validate registry.ts against Angular data-cy attributes
```

Available `ENV` values: `dev`, `dev2`, `test`, `uat`, `prod` (configs in `cypress/config/<env>.json`).

`autoui-guardian` manages selector sync and spec maintenance as part of every orchestrated workflow (`/implement-ticket`, `/fix-bug`, `/refactor`).
