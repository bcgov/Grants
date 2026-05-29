# OpenShift Environment Configuration for ArgoCD

This document provides the environment variable configurations needed for deploying the Grants Application Portal to OpenShift dev and test namespaces via ArgoCD.

## Environment Variables for ArgoCD Deployments

### Frontend Service Environment Variables

```yaml
env:
  - name: PORT
    value: "4000"
  - name: ENABLE_API_PROXY
    value: "true"  # Set to "false" if using OpenShift route-based API routing
  - name: BACKEND_SERVICE_URL
    value: "http://grants-api:5100"  # Internal service name for your backend
  - name: KEYCLOAK__CREDENTIALS__SECRET
    valueFrom:
      secretKeyRef:
        name: grants-keycloak-secret
        key: client-secret
  - name: KEYCLOAK__AUTHSERVERURL
    value: "https://dev.loginproxy.gov.bc.ca/auth"
  - name: KEYCLOAK__REALM
    value: "standard"
  - name: KEYCLOAK__RESOURCE
    value: "grants-portal-5361"
```

**Important Notes:**
- Environment variables are substituted at **runtime** by the Node.js server
- The server.js middleware processes JavaScript files and replaces `${VARIABLE_NAME}` placeholders
- All Keycloak environment variables are **required** for proper authentication
- Container logs will show environment variable values and substitution activity

### Backend Service Environment Variables

```yaml
env:
  - name: ASPNETCORE_ENVIRONMENT
    value: "Development"  # or "Production" for prod namespace
  - name: ConnectionStrings__DefaultConnection
    valueFrom:
      secretKeyRef:
        name: grants-database-secret
        key: connection-string
  - name: ConnectionStrings__Redis
    value: "redis:6379"  # Your Redis service name and port
  - name: Keycloak__AuthServerUrl
    value: "https://dev.loginproxy.gov.bc.ca/auth"
  - name: Keycloak__Realm
    value: "standard"
  - name: Keycloak__Resource
    value: "grants-portal-5361"
  - name: Keycloak__SslRequired
    value: "true"
  - name: Keycloak__ConfidentialPort
    value: "0"
  - name: Keycloak__Credentials__Secret
    valueFrom:
      secretKeyRef:
        name: grants-keycloak-secret
        key: client-secret
```

## Environment-Specific Configurations

### Development Environment

For **dev** namespace, use:
- `KEYCLOAK__AUTHSERVERURL`: `https://dev.loginproxy.gov.bc.ca/auth`
- `KEYCLOAK__RESOURCE`: `grants-portal-5361` (dev client)
- `ASPNETCORE_ENVIRONMENT`: `Development`

### Test Environment

For **test** namespace, use:
- `KEYCLOAK__AUTHSERVERURL`: `https://test.loginproxy.gov.bc.ca/auth` (if different)
- `KEYCLOAK__RESOURCE`: `grants-portal-5361-test` (test client, if different)
- `ASPNETCORE_ENVIRONMENT`: `Production`

## Required Secrets

Create these secrets in your OpenShift namespaces:

### Keycloak Secret
```yaml
apiVersion: v1
kind: Secret
metadata:
  name: grants-keycloak-secret
type: Opaque
stringData:
  client-secret: "your-actual-keycloak-client-secret"
```

### Database Secret
```yaml
apiVersion: v1
kind: Secret
metadata:
  name: grants-database-secret
type: Opaque
stringData:
  connection-string: "Host=your-postgres;Port=5432;Database=GrantsDB;Username=user;Password=pass"
```

## Service Configuration

### Frontend Service
- **Service Name**: `grants-web` (or your preferred name)
- **Port**: 4000
- **Container Port**: 4000

### Backend Service
- **Service Name**: `grants-api` (must match BACKEND_SERVICE_URL)
- **Port**: 5100
- **Container Port**: 5100

## Route Configuration

### Frontend Route
```yaml
apiVersion: route.openshift.io/v1
kind: Route
metadata:
  name: grants-web
spec:
  to:
    kind: Service
    name: grants-web
  port:
    targetPort: 4000
  tls:
    termination: edge
```

### Backend Route (if not using proxy)
```yaml
apiVersion: route.openshift.io/v1
kind: Route
metadata:
  name: grants-api
spec:
  path: /api
  to:
    kind: Service
    name: grants-api
  port:
    targetPort: 5100
  tls:
    termination: edge
```

## Container Image References

### Frontend
- **Image**: `your-registry/grants-applicant-portal-frontend:tag`
- **Build Context**: `applications/Grants.ApplicantPortal/src/Grants.ApplicantPortal.Frontend`
- **Dockerfile**: `applications/Grants.ApplicantPortal/src/Grants.ApplicantPortal.Frontend/Dockerfile`

### Backend
- **Image**: `your-registry/grants-applicant-portal-backend:tag`
- **Build Context**: `applications/Grants.ApplicantPortal/src/Grants.ApplicantPortal.Backend`
- **Dockerfile**: `applications/Grants.ApplicantPortal/src/Grants.ApplicantPortal.Backend/Dockerfile`

## Health Check Endpoints

### Frontend
- **Liveness**: `http://localhost:4000/healthz`
- **Readiness**: `http://localhost:4000/healthz/ready`

### Backend
- **Liveness**: `http://localhost:5100/health`
- **Readiness**: `http://localhost:5100/ready`

## Notes for ArgoCD Configuration

1. **Environment Variable Substitution**: The frontend uses environment variable substitution in the `environment.deploy.ts` file for Keycloak configuration. This happens at container startup.

2. **API Routing Strategy**: 
   - Set `ENABLE_API_PROXY=true` to use the frontend's built-in proxy
   - Set `ENABLE_API_PROXY=false` to rely on OpenShift routes

3. **Security**: Store sensitive values (client secrets, database passwords) as OpenShift secrets and reference them via `valueFrom.secretKeyRef`.

4. **Namespace Isolation**: Use different Keycloak clients or realms for dev vs test environments to maintain proper isolation.

## Verification Steps

After deployment:

1. **Frontend Health Check**: `curl https://your-frontend-route/healthz`
2. **Backend Health Check**: `curl https://your-backend-route/health`
3. **Keycloak Integration**: Check authentication flow works
4. **API Communication**: Verify frontend can communicate with backend