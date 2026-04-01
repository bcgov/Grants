# API Access Patterns Summary

## Overview

The Grants Applicant Portal API is designed to support **both direct API access and frontend applications** through a flexible JWT Bearer token authentication system.

## ?? **Authentication Architecture**

### Universal JWT Bearer Token Authentication
- **Token Type**: JWT Bearer tokens from Keycloak
- **Transport**: `Authorization: Bearer <token>` header
- **Works For**: Direct API calls, frontend applications, mobile apps, server-to-server

## ?? **Frontend Application Support**

### ? **Fully Supported For:**
- **Angular Applications** - Full OIDC integration with route guards
- **React Applications** - Component-level authorization
- **Vue Applications** - Reactive authentication state
- **Any SPA Framework** - Standard OIDC/JWT patterns

### ?? **What's Configured:**
- **CORS Support** - Environment-specific allowed origins
- **Keycloak OIDC** - Standard authorization code flow with PKCE
- **Role-based Authorization** - Maps directly to frontend permissions
- **Claims-based Access** - Rich user information for UI decisions

### ?? **Environment Configuration:**

#### Development
```json
{
  "Frontend": {
    "AllowedOrigins": [
      "http://localhost:4200",  // Angular default
      "http://localhost:3000",  // React default  
      "http://localhost:8080",  // Vue default
      "https://your-dev-domain.com"
    ]
  }
}
```

#### Production
```json
{
  "Frontend": {
    "AllowedOrigins": [
      "https://your-production-domain.com",
      "https://your-test-domain.com"
    ]
  }
}
```

## ?? **Direct API Access Support**

### ? **Fully Supported For:**
- **Server-to-Server Integration** - Service accounts with client credentials
- **API Testing Tools** - Postman, curl, etc. with JWT tokens
- **Mobile Applications** - Native apps with OIDC libraries
- **Desktop Applications** - Electron, .NET, etc.

### ?? **Access Patterns:**
```bash
# Direct API call with JWT token
curl -H "Authorization: Bearer <jwt-token>" \
     https://your-api-domain.com/api/profiles/123

# Service account authentication
curl -X POST https://your-keycloak-server.com/auth/realms/your-realm/protocol/openid-connect/token \
     -d "grant_type=client_credentials&client_id=service-account&client_secret=secret"
```

## ?? **Authorization Model Mapping**

The same authorization policies work for both access patterns:

| API Policy | Frontend Usage | Direct API Usage |
|------------|----------------|------------------|
| `RequireAuthenticatedUser` | Route guards, auth interceptors | Bearer token validation |
| `AdminOnly` | Admin UI components | Administrative API endpoints |
| `CanReadProfiles` | Profile view permissions | GET /api/profiles/* |
| `CanSubmitApplications` | Application forms | POST /api/applications |
| `CanReviewApplications` | Review interface | Application review endpoints |

## ?? **Usage Scenarios**

### 1. **Frontend + API (Most Common)**
```
[Angular/React/Vue] ? [Keycloak OIDC] ? [API with JWT Bearer]
```
- User logs in via frontend
- Frontend gets JWT token from Keycloak
- Frontend makes API calls with Bearer token
- API validates token and authorizes requests

### 2. **Direct API Access**
```
[Postman/curl/Mobile] ? [Keycloak Token Endpoint] ? [API with JWT Bearer]
```
- Client gets token directly from Keycloak
- Client makes API calls with Bearer token
- API validates token and authorizes requests

### 3. **Server-to-Server**
```
[Backend Service] ? [Keycloak Client Credentials] ? [API with JWT Bearer]
```
- Service uses client credentials flow
- Service gets service account token
- Service makes API calls with Bearer token

### 4. **Hybrid (Frontend + Direct)**
```
[Frontend] ? [Keycloak] ? [API]
     ?
[Mobile App] ? [Same Keycloak] ? [Same API]
```
- Multiple clients can use the same API
- Consistent authorization across all clients
- Single identity provider (Keycloak)

## ??? **Security Benefits**

### ? **Stateless Authentication**
- No server-side sessions
- Horizontally scalable
- Works with load balancers

### ? **Role-based Authorization**
- Fine-grained permissions
- Consistent across all clients
- Centralized in Keycloak

### ? **CORS Security**
- Environment-specific origin restrictions
- Credentials support for authenticated requests
- Protection against unauthorized cross-origin requests

## ?? **Documentation**

- **[Frontend Integration Guide](Frontend-Integration.md)** - Complete guide for SPA integration
- **[Authentication Guide](Authentication.md)** - Overall authentication documentation  
- **[Secrets Management](Secrets-Management.md)** - Configuration and secrets

## ?? **Recommendations**

### For **Frontend Applications**:
- ? Use the OIDC integration pattern
- ? Implement proper token refresh
- ? Use role-based route guards
- ? Handle authorization errors gracefully

### For **Direct API Access**:
- ? Use service accounts for server-to-server
- ? Implement proper token caching
- ? Handle token expiration
- ? Use appropriate scopes for your use case

### For **Both**:
- ? Always use HTTPS in production
- ? Implement proper error handling
- ? Log authentication/authorization events
- ? Monitor token usage and refresh patterns

Your API is architected to handle both patterns seamlessly! ??