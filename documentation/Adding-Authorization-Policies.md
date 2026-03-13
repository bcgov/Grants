# Adding New Authorization Policies

This guide shows how to easily add new authorization policies to the system.

## Example: Adding Organization-Based Authorization

Let's say you want to add organization-based authorization where users can only access data from their own organization.

### Step 1: Add Policy Constants

Update `AuthPolicies.cs`:

```csharp
#region Organization-Based Policies
/// <summary>
/// Policy requiring user to belong to same organization as resource
/// </summary>
public const string SameOrganization = "SameOrganization";

/// <summary>
/// Policy allowing cross-organization access for managers
/// </summary>
public const string CrossOrganizationManager = "CrossOrganizationManager";
#endregion
```

### Step 2: Add Policy Configuration

Update `AuthorizationConfigs.cs` by adding a new method:

```csharp
/// <summary>
/// Configure organization-based authorization policies
/// </summary>
private static void ConfigureOrganizationPolicies(AuthorizationOptions options)
{
    // Same organization access
    options.AddPolicy(AuthPolicies.SameOrganization, policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireAssertion(context =>
        {
            // This would typically check against route parameters or resource data
            var userOrg = context.User.FindFirst("organization")?.Value;
            return !string.IsNullOrEmpty(userOrg);
        });
    });

    // Cross-organization manager access
    options.AddPolicy(AuthPolicies.CrossOrganizationManager, policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireAssertion(context =>
            context.User.IsInRole("organization-manager") ||
            context.User.IsInRole("admin") ||
            context.User.IsInRole("system-admin"));
    });
}
```

### Step 3: Register New Policies

Update the main `AddAuthorizationPolicies` method:

```csharp
services.AddAuthorization(options =>
{
    // Configure default authorization policy
    ConfigureBasicPolicies(options);
    
    // Configure role-based policies
    ConfigureRolePolicies(options);
    
    // Configure resource-based policies
    ConfigureResourcePolicies(options);
    
    // Configure custom business logic policies
    ConfigureBusinessPolicies(options);
    
    // Configure organization-based policies - NEW!
    ConfigureOrganizationPolicies(options);
});
```

### Step 4: Use in Endpoints

```csharp
public override void Configure()
{
    Get("/api/organizations/{orgId}/data");
    Policies(AuthPolicies.SameOrganization);
}

public override void Configure()
{
    Get("/api/admin/all-organizations");
    Policies(AuthPolicies.CrossOrganizationManager);
}
```

## Example: Adding Time-Based Authorization

### Policy Constants
```csharp
#region Time-Based Policies
public const string BusinessHoursOnly = "BusinessHoursOnly";
public const string WeekdaysOnly = "WeekdaysOnly";
#endregion
```

### Policy Configuration
```csharp
/// <summary>
/// Configure time-based authorization policies
/// </summary>
private static void ConfigureTimePolicies(AuthorizationOptions options)
{
    options.AddPolicy(AuthPolicies.BusinessHoursOnly, policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireAssertion(context =>
        {
            var currentTime = DateTime.Now.TimeOfDay;
            var businessStart = new TimeSpan(9, 0, 0);  // 9 AM
            var businessEnd = new TimeSpan(17, 0, 0);   // 5 PM
            
            return currentTime >= businessStart && currentTime <= businessEnd;
        });
    });

    options.AddPolicy(AuthPolicies.WeekdaysOnly, policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireAssertion(context =>
        {
            var today = DateTime.Now.DayOfWeek;
            return today != DayOfWeek.Saturday && today != DayOfWeek.Sunday;
        });
    });
}
```

## Benefits of This Approach

1. **Maintainable**: All policies are organized in one place
2. **Discoverable**: Constants make policies easy to find
3. **Testable**: Each policy method can be unit tested
4. **Scalable**: Easy to add new categories of policies
5. **Type-Safe**: Using constants prevents typos in policy names

## Policy Categories

Organize your policies into logical categories:

- **Basic Policies**: Authentication requirements
- **Role-Based Policies**: Role hierarchy and permissions
- **Resource-Based Policies**: Access to specific resources
- **Business Logic Policies**: Complex business rules
- **Organization Policies**: Multi-tenant access control
- **Time-Based Policies**: Temporal access restrictions
- **Feature Policies**: Feature flag-based access
- **Geographic Policies**: Location-based restrictions

This approach ensures your authorization system remains organized and maintainable as it grows.