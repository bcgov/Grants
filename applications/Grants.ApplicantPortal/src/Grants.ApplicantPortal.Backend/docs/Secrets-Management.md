# Secrets Management Guide

This guide explains how to properly manage sensitive configuration values for the Grants Applicant Portal API.

## Overview

Sensitive values like client secrets, connection strings, and API keys should never be stored in configuration files that are committed to source control. Instead, use:

- **User Secrets** for local development
- **Environment Variables** for hosted environments
- **Azure Key Vault** for production secrets (recommended)

## Local Development Setup

### 1. Initialize User Secrets

From the `src/Grants.ApplicantPortal.API.Web` directory, run:

```bash
dotnet user-secrets init
```

This will create a unique identifier for your project's secrets.

### 2. Set Keycloak Secrets

Set the sensitive Keycloak configuration values:

```bash
# Set the client secret
dotnet user-secrets set "Keycloak:Credentials:Secret" "your-actual-client-secret"

# Optionally override other values for local development
dotnet user-secrets set "Keycloak:AuthServerUrl" "https://your-keycloak-server.com/auth"
dotnet user-secrets set "Keycloak:Realm" "your-realm"
dotnet user-secrets set "Keycloak:Resource" "your-client-id"
```

### 3. Set Database Connection Strings (if needed)

```bash
# Override connection strings for local development
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Database=GrantsPortal;Trusted_Connection=true;"
```

### 4. Verify Secrets

List all configured secrets:

```bash
dotnet user-secrets list
```

### 5. Remove Secrets (if needed)

```bash
# Remove a specific secret
dotnet user-secrets remove "Keycloak:Credentials:Secret"

# Clear all secrets
dotnet user-secrets clear
```

## Production Environment Setup

### Option 1: Environment Variables

Set environment variables in your hosting environment:

```bash
# Keycloak Configuration
KEYCLOAK__CREDENTIALS__SECRET=your-production-secret
KEYCLOAK__AUTHSERVERURL=https://your-keycloak-server.com/auth
KEYCLOAK__REALM=your-realm
KEYCLOAK__RESOURCE=your-client-id

# Connection Strings
CONNECTIONSTRINGS__DEFAULTCONNECTION=your-production-connection-string
```

### Option 2: Azure Key Vault (Recommended)

1. **Create Azure Key Vault**:
   ```bash
   az keyvault create --name "grants-portal-kv" --resource-group "your-rg" --location "canadacentral"
   ```

2. **Add Secrets to Key Vault**:
   ```bash
   az keyvault secret set --vault-name "grants-portal-kv" --name "Keycloak--Credentials--Secret" --value "your-secret"
   az keyvault secret set --vault-name "grants-portal-kv" --name "ConnectionStrings--DefaultConnection" --value "your-connection-string"
   ```

3. **Configure App Service**:
   - Enable Managed Identity for your App Service
   - Grant the Managed Identity access to Key Vault
   - Add Key Vault references in App Service configuration

## Docker Environment

For Docker deployments, use environment variables or Docker secrets:

### docker-compose.yml example:

```yaml
version: '3.8'
services:
  web:
    image: grants-portal-api
    environment:
      - KEYCLOAK__CREDENTIALS__SECRET=${KEYCLOAK_SECRET}
      - KEYCLOAK__AUTHSERVERURL=${KEYCLOAK_URL}
      - KEYCLOAK__REALM=${KEYCLOAK_REALM}
      - KEYCLOAK__RESOURCE=${KEYCLOAK_CLIENT_ID}
    env_file:
      - .env.production
```

### .env.production example:

```bash
KEYCLOAK_SECRET=your-production-secret
KEYCLOAK_URL=https://your-keycloak-server.com/auth
KEYCLOAK_REALM=your-realm
KEYCLOAK_CLIENT_ID=your-client-id
```

## Configuration Hierarchy

ASP.NET Core loads configuration in this order (later sources override earlier ones):

1. `appsettings.json`
2. `appsettings.{Environment}.json`
3. User Secrets (Development only)
4. Environment Variables
5. Command Line Arguments

## Security Best Practices

### ? Do:
- Use User Secrets for local development
- Use Environment Variables or Key Vault for production
- Set different client IDs/secrets per environment
- Use HTTPS-only in production (`SslRequired: true`)
- Rotate secrets regularly

### ? Don't:
- Commit secrets to source control
- Use the same secrets across environments
- Store secrets in configuration files
- Share secrets via email or chat
- Use weak or default secrets

## Environment-Specific Configuration

### Development
- Uses `appsettings.Development.json`
- Loads User Secrets automatically
- Debug logging enabled
- May use development Keycloak realm

### Staging
- Uses `appsettings.Staging.json`
- Environment variables for secrets
- Minimal logging
- Uses staging Keycloak realm

### Production
- Uses `appsettings.Production.json`
- Environment variables or Key Vault for secrets
- Error/Warning logging only
- Uses production Keycloak realm

## Troubleshooting

### Configuration Not Loading
1. Check the environment (`ASPNETCORE_ENVIRONMENT`)
2. Verify secrets are set correctly (`dotnet user-secrets list`)
3. Check environment variable names (use `__` for nested properties)
4. Ensure UserSecretsId is set in the project file

### Keycloak Authentication Failing
1. Verify the client secret is correct
2. Check the realm and client ID match Keycloak configuration
3. Ensure the Keycloak server URL is accessible
4. Verify SSL certificate if using HTTPS

### Logging Configuration Issues
1. Check `ASPNETCORE_ENVIRONMENT` matches your appsettings file
2. Verify logging levels in environment-specific configuration
3. Ensure log file paths are writable

## Team Setup

Each team member should:

1. Clone the repository
2. Initialize user secrets: `dotnet user-secrets init`
3. Get the development secrets from your team lead
4. Set secrets using `dotnet user-secrets set`
5. Never commit user secrets or sensitive values

## Automated Deployment

For CI/CD pipelines:

1. Store secrets in your CI/CD system's secret management
2. Set environment variables during deployment
3. Use deployment-specific configurations
4. Implement secret rotation procedures

## Monitoring

- Monitor secret expiration dates
- Set up alerts for authentication failures
- Log configuration loading issues (without exposing values)
- Regularly audit secret access