# Keycloak Authentication Placeholder Values

This document defines the standard placeholder values that should be used across all documentation for Keycloak authentication configuration.

## 📋 Standard Placeholder Values

### 🔑 **Core Keycloak Configuration**
- **Keycloak Server URL**: `https://your-keycloak-server.com/auth`
- **Realm**: `your-realm`
- **Client ID**: `your-client-id`
- **Client Secret**: `your-actual-client-secret` or `your-client-secret-here`

### 🌍 **Environment-Specific Placeholders**

#### Development Environment
- **Keycloak Server**: `https://your-dev-keycloak-server.com/auth`
- **Client ID**: `your-client-id-dev` (if different from production)
- **API Domain**: `https://localhost:7000` (local development)
- **Frontend Domain**: `https://your-dev-domain.com`

#### Production Environment
- **Keycloak Server**: `https://your-production-keycloak-server.com/auth`
- **Client ID**: `your-client-id`
- **API Domain**: `https://your-api-domain.com`
- **Frontend Domain**: `https://your-production-domain.com`

#### Test/Staging Environment
- **Frontend Domain**: `https://your-test-domain.com`

### 🔗 **Full Endpoint URLs**
- **Authorization Endpoint**: `https://your-keycloak-server.com/auth/realms/your-realm/protocol/openid-connect/auth`
- **Token Endpoint**: `https://your-keycloak-server.com/auth/realms/your-realm/protocol/openid-connect/token`
- **UserInfo Endpoint**: `https://your-keycloak-server.com/auth/realms/your-realm/protocol/openid-connect/userinfo`

### 🎯 **CORS Origins**
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

## 🔧 **Configuration Examples**

### appsettings.json (Base Configuration)
```json
{
  "Keycloak": {
    "AuthServerUrl": "https://your-keycloak-server.com/auth",
    "Realm": "your-realm",
    "SslRequired": true,
    "Resource": "your-client-id",
    "ConfidentialPort": 0,
    "Credentials": {
      "Secret": "your-client-secret-here"
    }
  }
}
```

### Environment Variables
```bash
# Keycloak Configuration
KEYCLOAK__CREDENTIALS__SECRET=your-production-secret
KEYCLOAK__AUTHSERVERURL=https://your-keycloak-server.com/auth
KEYCLOAK__REALM=your-realm
KEYCLOAK__RESOURCE=your-client-id

# Connection Strings
CONNECTIONSTRINGS__DEFAULTCONNECTION=your-production-connection-string
```

### User Secrets (Development)
```bash
# Set the client secret
dotnet user-secrets set "Keycloak:Credentials:Secret" "your-actual-client-secret"

# Optionally override other values for local development
dotnet user-secrets set "Keycloak:AuthServerUrl" "https://your-dev-keycloak-server.com/auth"
dotnet user-secrets set "Keycloak:Realm" "your-realm"
dotnet user-secrets set "Keycloak:Resource" "your-client-id"
```

### Docker Environment File
```bash
KEYCLOAK_SECRET=your-production-secret
KEYCLOAK_URL=https://your-keycloak-server.com/auth
KEYCLOAK_REALM=your-realm
KEYCLOAK_CLIENT_ID=your-client-id
```

## ✅ **Updated Documentation Files**

The following documentation files have been updated to use these standard placeholder values:

- ✅ **Authentication.md** - Main authentication documentation
- ✅ **Frontend-Integration.md** - Frontend integration guide
- ✅ **Secrets-Management.md** - Secrets management guide
- ✅ **API-Access-Patterns.md** - API access patterns
- ✅ **Adding-Authorization-Policies.md** - Already had generic examples

## 🔍 **What Was Changed**

### Before (Environment-Specific Values)
- `https://dev.loginproxy.gov.bc.ca/auth`
- `https://loginproxy.gov.bc.ca/auth`
- `grants-portal-5361`
- `standard`
- Specific BC Gov domains

### After (Generic Placeholders)
- `https://your-keycloak-server.com/auth`
- `https://your-dev-keycloak-server.com/auth`
- `your-client-id`
- `your-realm`
- Generic domain placeholders

## 📝 **Usage Guidelines**

### ✅ **Do:**
- Use these exact placeholder values in all documentation
- Be consistent across all files
- Use descriptive placeholder names that indicate what should be replaced
- Include both development and production examples where relevant

### ❌ **Don't:**
- Mix real values with placeholders in documentation
- Use environment-specific values in generic examples
- Use placeholder values that are too generic (`example.com`, `test`, etc.)
- Forget to update all related documentation when changing placeholders

## 🚀 **Implementation Teams**

When implementing this system, teams should:

1. **Replace all placeholder values** with their actual Keycloak configuration
2. **Set up proper secrets management** using the patterns shown in Secrets-Management.md
3. **Configure environment-specific values** in appropriate configuration files
4. **Test authentication flows** with their actual Keycloak server
5. **Update any custom documentation** to use these same placeholder patterns

This standardization ensures consistency across all documentation and makes it easier for teams to understand what values need to be configured for their specific environment.
