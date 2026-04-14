# Keycloak OIDC Authentication Implementation

## Overview
This implementation adds JWT Bearer token authentication using Keycloak as the identity provider for the Grants Applicant Portal API.

## Configuration

### Secrets Management ??
**Important**: Sensitive configuration values are managed through user secrets for development and environment variables for production. Never commit secrets to source control.

### Local Development Setup

1. **Initialize User Secrets** (first time only):
   ```bash
   cd src/Grants.ApplicantPortal.API.Web
   dotnet user-secrets init
   ```

2. **Set Required Secrets**:
   ```bash
   # Set the Keycloak client secret
   dotnet user-secrets set "Keycloak:Credentials:Secret" "your-actual-secret"
   
   # Optionally override other values for local development
   dotnet user-secrets set "Keycloak:AuthServerUrl" "https://your-dev-keycloak-server.com/auth"
   dotnet user-secrets set "Keycloak:Realm" "your-realm"
   dotnet user-secrets set "Keycloak:Resource" "your-client-id"
   ```

3. **Quick Setup** (Alternative):
   Run the setup script from the repository root:
   ```bash
   # Linux/Mac
   ./scripts/setup-dev-secrets.sh
   
   # Windows PowerShell
   .\scripts\setup-dev-secrets.ps1
   ```

### Configuration Structure

#### appsettings.json (Placeholder Values)
Contains non-sensitive default values and placeholders:

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

#### appsettings.Development.json
Environment-specific non-sensitive overrides:

```json
{
  "Keycloak": {
    "AuthServerUrl": "https://your-dev-keycloak-server.com/auth",
    "Realm": "your-realm",
    "Resource": "your-client-id-dev"
  }
}
```

#### appsettings.Production.json
Production environment configuration:

```json
{
  "Keycloak": {
    "AuthServerUrl": "https://your-production-keycloak-server.com/auth",
    "Realm": "your-realm",
    "Resource": "your-client-id"
  }
}
```

### Production Environment Setup

#### Environment Variables
Set these environment variables in your hosting environment:

```bash
KEYCLOAK__CREDENTIALS__SECRET=your-production-secret
KEYCLOAK__AUTHSERVERURL=https://your-production-keycloak-server.com/auth
KEYCLOAK__REALM=your-realm
KEYCLOAK__RESOURCE=your-client-id
```

#### Azure Key Vault (Recommended)
For production deployments, use Azure Key Vault:

```bash
az keyvault secret set --vault-name "grants-portal-kv" \
  --name "Keycloak--Credentials--Secret" \
  --value "your-production-secret"
```

## Architecture

The authentication and authorization system is separated into dedicated configuration classes:

- **`AuthenticationConfigs.cs`** - Handles JWT Bearer authentication with Keycloak
- **`AuthorizationConfigs.cs`** - Manages all authorization policies (separated for maintainability)
- **`AuthPolicies.cs`** - Constants for policy names
- **`KeycloakClaimsHelper.cs`** - Helper methods for working with Keycloak claims

## How Authentication Works

1. **JWT Bearer Authentication**: The API validates JWT tokens issued by Keycloak
2. **Authority**: Configured per environment (dev/staging/production)
3. **Audience**: Your client ID configured in Keycloak
4. **Token Validation**: Validates issuer, audience, lifetime, and signing key
5. **Claims Transformation**: Automatically transforms Keycloak roles into standard ASP.NET Core claims

## Authorization Policies

The authorization policies are organized into categories in `AuthorizationConfigs.cs`:

### Basic Authentication Policies
- **`RequireAuthenticatedUser`**: Default policy requiring valid JWT token
- **`RequireRealmRole`**: Requires user to have realm roles from Keycloak

### Role-Based Policies
- **`AdminOnly`**: Requires 'admin' role
- **`UserOrAdmin`**: Requires 'user' OR 'admin' role
- **`SystemAdmin`**: Requires 'system-admin' role (highest level)
- **`ProgramManager`**: Requires 'program-manager', 'admin', or 'system-admin' role
- **`GrantOfficer`**: Requires 'grant-officer', 'program-manager', 'admin', or 'system-admin' role

### Resource-Based Policies
- **`CanReadProfiles`**: Can read profile data
- **`CanManageProfiles`**: Can manage profile data
- **`CanSubmitApplications`**: Can submit grant applications
- **`CanReviewApplications`**: Can review grant applications
- **`CanManageSystem`**: Can manage system settings

### Business Logic Policies
- **`RequireVerifiedEmail`**: Requires verified email address
- **`RequireCompleteProfile`**: Requires complete user profile
- **`RequireTermsAcceptance`**: Requires terms of service acceptance
- **`FullyVerifiedUser`**: Combines email verification, complete profile, and terms acceptance

### Policy Constants
Use the `AuthPolicies` class to reference policies by name:

```csharp
public static class AuthPolicies
{
    // Basic policies
    public const string RequireAuthenticatedUser = "RequireAuthenticatedUser";
    public const string RequireRealmRole = "RequireRealmRole";
    
    // Role-based policies
    public const string AdminOnly = "AdminOnly";
    public const string UserOrAdmin = "UserOrAdmin";
    public const string SystemAdmin = "SystemAdmin";
    public const string ProgramManager = "ProgramManager";
    public const string GrantOfficer = "GrantOfficer";
    
    // Resource-based policies
    public const string CanReadProfiles = "CanReadProfiles";
    public const string CanManageProfiles = "CanManageProfiles";
    public const string CanSubmitApplications = "CanSubmitApplications";
    public const string CanReviewApplications = "CanReviewApplications";
    public const string CanManageSystem = "CanManageSystem";
    
    // Business logic policies
    public const string RequireVerifiedEmail = "RequireVerifiedEmail";
    public const string RequireCompleteProfile = "RequireCompleteProfile";
    public const string RequireTermsAcceptance = "RequireTermsAcceptance";
    public const string FullyVerifiedUser = "FullyVerifiedUser";
}
```

## Using Authentication in FastEndpoints

### For Authenticated Endpoints
```csharp
public override void Configure()
{
    Get("/api/profiles/{profileId}");
    Policies(AuthPolicies.RequireAuthenticatedUser); // Requires valid JWT token
}
```

### For Role-based Authorization
```csharp
public override void Configure()
{
    Post("/api/admin/users");
    Policies(AuthPolicies.AdminOnly); // Requires 'admin' role
}
```

### For Resource-based Authorization
```csharp
public override void Configure()
{
    Get("/api/profiles");
    Policies(AuthPolicies.CanReadProfiles); // Can read profiles
}
```

### For Business Logic Authorization
```csharp
public override void Configure()
{
    Post("/api/applications");
    Policies(AuthPolicies.FullyVerifiedUser); // Requires fully verified user
}
```

### For Public Endpoints
```csharp
public override void Configure()
{
    Get("/System/health");
    AllowAnonymous(); // No authentication required
}
```

### Multiple Policies
```csharp
public override void Configure()
{
    Put("/api/users/{id}");
    Policies(AuthPolicies.UserOrAdmin); // Requires 'user' OR 'admin' role
}
```

## Adding New Policies

To add new authorization policies, update the `AuthorizationConfigs.cs` file:

```csharp
// Add to the appropriate configuration method
options.AddPolicy("NewPolicyName", policy =>
{
    policy.RequireAuthenticatedUser();
    policy.RequireAssertion(context =>
    {
        // Your custom authorization logic here
        return context.User.IsInRole("custom-role");
    });
});
```

Then add the constant to `AuthPolicies.cs`:

```csharp
public const string NewPolicyName = "NewPolicyName";
```

## Working with Keycloak Claims

### KeycloakClaimsHelper Extension Methods
Use the `KeycloakClaimsHelper` class to easily work with Keycloak claims:

```csharp
public override async Task HandleAsync(MyRequest request, CancellationToken ct)
{
    var user = HttpContext.User;
    
    // Get user information
    var username = user.GetPreferredUsername();
    var email = user.GetEmail();
    var fullName = user.GetFullName();
    var userId = user.GetSubject();
    
    // Check roles
    var realmRoles = user.GetRealmRoles();
    var hasAdminRole = user.HasRealmRole("admin");
    var hasResourceRole = user.HasResourceRole("grants-portal-5361", "user");
    
    // Your endpoint logic here...
}
```

### Available Helper Methods
- `GetPreferredUsername()` - Keycloak username
- `GetEmail()` - User email address
- `GetFullName()` - User's full name
- `GetGivenName()` - User's first name
- `GetFamilyName()` - User's last name
- `GetSubject()` - Unique user identifier
- `GetRealmRoles()` - All realm roles
- `GetResourceRoles(resource)` - Roles for specific resource/client
- `HasRealmRole(role)` - Check for specific realm role  
- `HasResourceRole(resource, role)` - Check for specific resource role

## Keycloak Token Structure

Keycloak JWT tokens contain the following relevant claims:
- `preferred_username` - Username
- `email` - Email address
- `given_name` - First name
- `family_name` - Last name
- `name` - Full name
- `sub` - Subject (unique user ID)
- `realm_access.roles` - Array of realm roles
- `resource_access.{client}.roles` - Array of client-specific roles

## Client Usage

Clients must include the JWT token in the Authorization header:

```
Authorization: Bearer <your-jwt-token>
```

## Token Acquisition

Clients obtain tokens from your Keycloak server:
- **Development Token Endpoint**: `https://your-dev-keycloak-server.com/auth/realms/your-realm/protocol/openid-connect/token`
- **Production Token Endpoint**: `https://your-production-keycloak-server.com/auth/realms/your-realm/protocol/openid-connect/token`
- **Authorization Endpoint**: Same base URL with `/protocol/openid-connect/auth`

## Security Considerations

1. **HTTPS Only**: The configuration enforces HTTPS for token validation
2. **Environment Separation**: Different secrets and endpoints per environment
3. **Secret Management**: Sensitive values stored in user secrets/environment variables
4. **Token Lifetime**: Tokens have built-in expiration times
5. **Clock Skew**: 5-minute tolerance for time differences
6. **Signature Validation**: All tokens are cryptographically verified
7. **Claims Transformation**: Keycloak roles are transformed into standard ASP.NET Core role claims
8. **Separation of Concerns**: Authentication and authorization are properly separated

## Files Modified/Added

- `KeycloakConfiguration.cs` - Configuration model
- `AuthenticationConfigs.cs` - Authentication setup (JWT Bearer only)
- `AuthorizationConfigs.cs` - Authorization policies (separated for maintainability)
- `AuthPolicies.cs` - Policy constants (organized by categories)
- `KeycloakClaimsHelper.cs` - Helper for working with Keycloak claims
- `RequireAuthAttribute.cs` - Authorization attribute
- `appsettings.json` - Contains placeholder values only
- `appsettings.Development.json` - Development environment overrides
- `appsettings.Production.json` - Production environment overrides
- Updated middleware pipeline to include authentication/authorization
- Updated service registration to call both authentication and authorization configs
- Updated endpoints to require authentication where appropriate

## Documentation

- [Secrets Management Guide](Secrets-Management.md) - Comprehensive guide for managing sensitive configuration
- [Adding Authorization Policies](Adding-Authorization-Policies.md) - Guide for extending authorization

## Team Setup

Each new team member should:

1. Clone the repository
2. Run the setup script: `./scripts/setup-dev-secrets.sh` (or `.ps1` for Windows)
3. Verify configuration: `dotnet user-secrets list`
4. Start the application: `dotnet run`

## Testing

- **System endpoints** (`/System/health`, `/System/info`) remain public
- **Profile endpoints** now require authentication
- **UserInfo endpoint** (`/Auth/userinfo`) demonstrates claims usage
- Use Swagger UI or Postman with valid JWT tokens for testing
- Test different role combinations using the defined policies

## Troubleshooting

If authentication isn't working:
1. Check that user secrets are set: `dotnet user-secrets list`
2. Verify the environment is correct: `echo $ASPNETCORE_ENVIRONMENT`
3. Check logs for authentication errors
4. Ensure Keycloak server is accessible
5. Verify client configuration in Keycloak matches your settings

For detailed troubleshooting, see the [Secrets Management Guide](Secrets-Management.md)