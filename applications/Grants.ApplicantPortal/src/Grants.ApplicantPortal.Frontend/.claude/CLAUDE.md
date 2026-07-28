# Grants Applicant Portal — Frontend

Angular 20 SPA for BC Government grants applicants. Served by an Express.js container on OpenShift.

## Tech Stack

- **Framework**: Angular 20 (standalone components, signals-ready)
- **Auth**: Keycloak via `angular-auth-oidc-client` (OIDC/OAuth2)
- **Styling**: BC Gov Bootstrap v5 theme + SCSS
- **Icons**: Font Awesome 6
- **Testing**: Karma + Jasmine
- **Server**: Express.js (`server.js`) — SPA serving, API proxy, rate limiting, health endpoints
- **Container**: Docker multi-stage (Node 22-slim), OpenShift deployment
- **Analytics**: Matomo (`ngx-matomo-client`)

**Rule**: Business logic belongs in `core/services`. UI logic belongs in `features`. Reusable UI belongs in `shared`. Never import `features` from `core` or `shared`.

## Key Files

| File | Purpose |
|---|---|
| `src/app/app.routes.ts` | All client-side routes |
| `src/app/app.config.ts` | App-level Angular providers |
| `src/app/api.service.ts` | HTTP client wrapper for all API calls |
| `src/app/core/auth/auth.config.ts` | Keycloak realm/client config |
| `src/app/core/guards/auth.guard.ts` | Route authentication guard |
| `src/app/core/interceptors/auth.interceptor.ts` | JWT token injection |
| `src/environments/environment.ts` | Dev environment variables |
| `src/environments/environment.deploy.ts` | Production environment variables |
| `server.js` | Express SPA server — proxy, rate limiting, health checks |
| `Dockerfile` | Multi-stage Docker build |

## Environment Variables (server.js / container)

| Variable | Default | Purpose |
|---|---|---|
| `PORT` | `4200` | HTTP port |
| `ENABLE_API_PROXY` | `false` | Enable `/api` → backend proxy |
| `BACKEND_SERVICE_URL` | `http://backend:5100` | Backend API base URL |
| `KEYCLOAK__AUTHSERVERURL` | — | Keycloak server URL |
| `KEYCLOAK__REALM` | — | Keycloak realm |
| `KEYCLOAK__RESOURCE` | — | Keycloak client ID |
| `MATOMO__URL` | — | Matomo analytics URL |
| `MATOMO__SITEID` | — | Matomo site ID |
| `RATE_LIMIT_MAX` | — | Max requests per window |
| `RATE_LIMIT_WINDOW_MS` | — | Rate limit window in ms |

## Auth Flow

1. Unauthenticated users hit `auth.guard.ts` → redirected to `/login`
2. Login page initiates Keycloak OIDC redirect
3. Keycloak redirects back to `/callback`
4. `auth.interceptor.ts` attaches `Authorization: Bearer <token>` to every outgoing HTTP request
5. Token refresh is handled automatically by `angular-auth-oidc-client`

## API Calls

All backend calls go through `api.service.ts`. In local dev, the Angular dev server proxies `/api` to the backend. In production, `server.js` handles the proxy when `ENABLE_API_PROXY=true`.

Use `/api-call <endpoint description>` to add a new typed API method following the existing pattern.

## Scaffolding

| Task | Command |
| --- | --- |
| New feature page | `/new-feature <name>` |
| New shared component | `/new-shared-component <name>` |
| New core service | `/new-service <name>` |
| New API method | `/api-call <endpoint description>` |
| Audit env var wiring | `/env-check [VAR_NAME]` |

- Unit tests live next to their source file (`*.spec.ts`)
- Use `TestBed` for Angular component/service tests
- Run a single spec: `ng test --include='**/foo.component.spec.ts'`

## Health Endpoints (server.js)

- `GET /healthz` — liveness probe
- `GET /healthz/ready` — readiness probe
