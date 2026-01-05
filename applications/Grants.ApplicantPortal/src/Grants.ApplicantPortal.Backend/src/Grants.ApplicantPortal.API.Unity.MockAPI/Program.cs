using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();

// Unity Mock API Endpoints
app.MapGet("/api/v1/profiles/{profileId}", (Guid profileId, string? provider = null) =>
{
    var actualProvider = provider ?? "DGP"; // Default to DGP if not specified
    
    var response = new
    {
        ProfileId = profileId,
        Provider = actualProvider,
        Key = "PROFILE",
        Source = "Unity Mock API",
        PopulatedAt = DateTime.UtcNow,
        PopulatedBy = "UNITY",
        IsMockData = true,
        Data = new
        {
            PersonalInfo = new
            {
                FirstName = "John",
                LastName = "Doe",
                Email = "john.doe@unity.gov",
                Phone = "+1-555-0123",
                EmployeeId = "UNI-12345"
            }
        }
    };
    
    return Results.Ok(response);
})
.WithName("GetProfile")
.WithOpenApi();

app.MapGet("/api/v1/profiles/{profileId}/employment", (Guid profileId, string? provider = null) =>
{
    var actualProvider = provider ?? "DGP"; // Default to DGP if not specified
    
    var response = new
    {
        ProfileId = profileId,
        Provider = actualProvider,
        Key = "EMPLOYMENT",
        Source = "Unity Mock API",
        PopulatedAt = DateTime.UtcNow,
        PopulatedBy = "UNITY",
        IsMockData = true,
        Data = new
        {
            Employment = new
            {
                Department = "Department of Health",
                Position = "Senior Analyst",
                StartDate = "2020-01-15",
                EmployeeId = "UNI-12345",
                Manager = "Jane Smith",
                Location = "Building A, Room 205"
            }
        }
    };
    
    return Results.Ok(response);
})
.WithName("GetEmployment")
.WithOpenApi();

app.MapGet("/api/v1/profiles/{profileId}/security", (Guid profileId, string? provider = null) =>
{
    var actualProvider = provider ?? "DGP"; // Default to DGP if not specified
    
    var response = new
    {
        ProfileId = profileId,
        Provider = actualProvider,
        Key = "SECURITY",
        Source = "Unity Mock API",
        PopulatedAt = DateTime.UtcNow,
        PopulatedBy = "UNITY",
        IsMockData = true,
        Data = new
        {
            Security = new
            {
                ClearanceLevel = "Secret",
                BadgeNumber = "B789456",
                LastUpdated = DateTime.UtcNow,
                ExpiryDate = DateTime.UtcNow.AddYears(2),
                AccessLevel = "Level 3"
            }
        }
    };
    
    return Results.Ok(response);
})
.WithName("GetSecurity")
.WithOpenApi();

app.MapGet("/api/v1/profiles/{profileId}/contacts", (Guid profileId, string? provider = null) =>
{
    var actualProvider = provider ?? "DGP"; // Default to DGP if not specified
    
    var response = new
    {
        ProfileId = profileId,
        Provider = actualProvider,
        Key = "CONTACTS",
        Source = "Unity Mock API",
        PopulatedAt = DateTime.UtcNow,
        PopulatedBy = "UNITY",
        IsMockData = true,
        Data = new
        {
            Contacts = new[]
            {
                new
                {
                    ContactId = Guid.NewGuid(),
                    Name = "John Doe",
                    Type = "PRIMARY",
                    IsPrimary = true,
                    Title = "Director",
                    Email = "john.doe@unity.gov",
                    PhoneNumber = "+1-555-0123",
                    LastUpdated = DateTime.UtcNow.AddDays(-5)
                },
                new
                {
                    ContactId = Guid.NewGuid(),
                    Name = "Jane Smith",
                    Type = "GRANTS",
                    IsPrimary = false,
                    Title = "Grants Manager",
                    Email = "jane.smith@unity.gov",
                    PhoneNumber = "+1-555-0124",
                    LastUpdated = DateTime.UtcNow.AddDays(-2)
                }
            }
        }
    };
    
    return Results.Ok(response);
})
.WithName("GetContacts")
.WithOpenApi();

app.MapGet("/api/v1/profiles/{profileId}/addresses", (Guid profileId, string? provider = null) =>
{
    var actualProvider = provider ?? "DGP"; // Default to DGP if not specified
    
    var response = new
    {
        ProfileId = profileId,
        Provider = actualProvider,
        Key = "ADDRESSES",
        Source = "Unity Mock API",
        PopulatedAt = DateTime.UtcNow,
        PopulatedBy = "UNITY",
        IsMockData = true,
        Data = new
        {
            Addresses = new[]
            {
                new
                {
                    AddressId = Guid.NewGuid(),
                    Type = "MAILING",
                    IsPrimary = true,
                    Address = "1234 Government St",
                    City = "Victoria",
                    Province = "BC",
                    PostalCode = "V8W 1A4",
                    Country = "Canada",
                    LastUpdated = DateTime.UtcNow.AddDays(-10)
                },
                new
                {
                    AddressId = Guid.NewGuid(),
                    Type = "PHYSICAL",
                    IsPrimary = false,
                    Address = "5678 Main St",
                    City = "Vancouver",
                    Province = "BC",
                    PostalCode = "V6B 2C3",
                    Country = "Canada",
                    LastUpdated = DateTime.UtcNow.AddDays(-7)
                }
            }
        }
    };
    
    return Results.Ok(response);
})
.WithName("GetAddresses")
.WithOpenApi();

app.MapGet("/api/v1/profiles/{profileId}/organization", (Guid profileId, string? provider = null) =>
{
    var actualProvider = provider ?? "DGP"; // Default to DGP if not specified
    
    var response = new
    {
        ProfileId = profileId,
        Provider = actualProvider,
        Key = "ORGINFO",
        Source = "Unity Mock API",
        PopulatedAt = DateTime.UtcNow,
        PopulatedBy = "UNITY",
        IsMockData = true,
        Data = new
        {
            Organization = new
            {
                OrganizationId = Guid.NewGuid(),
                Name = "Unity Department",
                OrganizationType = "Government",
                LegalName = "Unity Government Department",
                Status = "Active",
                OrganizationNumber = "UNI-ORG-001",
                Founded = "1995-04-01",
                Mission = "Serving citizens through innovative government services",
                ServiceAreas = new[] { "Public Service", "Technology", "Innovation" },
                LastUpdated = DateTime.UtcNow.AddDays(-1)
            }
        }
    };
    
    return Results.Ok(response);
})
.WithName("GetOrganization")
.WithOpenApi();

app.MapGet("/api/v1/profiles/{profileId}/submissions", (Guid profileId, string? provider = null) =>
{
    var actualProvider = provider ?? "DGP"; // Default to DGP if not specified
    
    var response = new
    {
        ProfileId = profileId,
        Provider = actualProvider,
        Key = "SUBMISSIONS",
        Source = "Unity Mock API",
        PopulatedAt = DateTime.UtcNow,
        PopulatedBy = "UNITY",
        IsMockData = true,
        Data = new
        {
            Submissions = new[]
            {
                new
                {
                    SubmissionId = Guid.NewGuid(),
                    ProgramName = "Health Innovation Grant",
                    Status = "Under Review",
                    SubmittedDate = DateTime.UtcNow.AddDays(-15),
                    Amount = 50000.00m,
                    ApplicationNumber = "HIG-2024-001",
                    LastUpdated = DateTime.UtcNow.AddDays(-2)
                },
                new
                {
                    SubmissionId = Guid.NewGuid(),
                    ProgramName = "Technology Development Fund",
                    Status = "Approved",
                    SubmittedDate = DateTime.UtcNow.AddDays(-45),
                    Amount = 75000.00m,
                    ApplicationNumber = "TDF-2024-003",
                    LastUpdated = DateTime.UtcNow.AddDays(-5)
                }
            }
        }
    };
    
    return Results.Ok(response);
})
.WithName("GetSubmissions")
.WithOpenApi();

app.MapGet("/api/v1/profiles/{profileId}/payments", (Guid profileId, string? provider = null) =>
{
    var actualProvider = provider ?? "DGP"; // Default to DGP if not specified
    
    var response = new
    {
        ProfileId = profileId,
        Provider = actualProvider,
        Key = "PAYMENTS",
        Source = "Unity Mock API",
        PopulatedAt = DateTime.UtcNow,
        PopulatedBy = "UNITY",
        IsMockData = true,
        Data = new
        {
            Payments = new[]
            {
                new
                {
                    PaymentId = Guid.NewGuid(),
                    SubmissionId = Guid.NewGuid(),
                    Amount = 75000.00m,
                    PaymentDate = DateTime.UtcNow.AddDays(-30),
                    PaymentMethod = "Direct Deposit",
                    Status = "Completed",
                    TransactionId = "TXN-2024-001"
                },
                new
                {
                    PaymentId = Guid.NewGuid(),
                    SubmissionId = Guid.NewGuid(),
                    Amount = 25000.00m,
                    PaymentDate = DateTime.UtcNow.AddDays(-10),
                    PaymentMethod = "Wire Transfer",
                    Status = "Processing",
                    TransactionId = "TXN-2024-002"
                }
            }
        }
    };
    
    return Results.Ok(response);
})
.WithName("GetPayments")
.WithOpenApi();

app.MapGet("/api/v1/profiles/{profileId}/data", (Guid profileId, string? provider = null) =>
{
    var actualProvider = provider ?? "DGP"; // Default to DGP if not specified
    
    var response = new
    {
        ProfileId = profileId,
        Provider = actualProvider,
        Key = "DEFAULT",
        Source = "Unity Mock API",
        PopulatedAt = DateTime.UtcNow,
        PopulatedBy = "UNITY",
        IsMockData = true,
        Data = new
        {
            Message = "Unity data available for:",
            AvailableProviders = new[] { "DGP", "ABC" },
            AvailableKeys = new[] { "Profile", "Employment", "Security", "Contacts", "Addresses", "OrgInfo", "Submissions", "Payments" },
            Instructions = "Use Provider and Key parameters to get specific Unity data",
            Examples = new[]
            {
                "Provider=DGP, Key=Profile - GET /api/v1/profiles/{profileId}?provider=DGP",
                "Provider=DGP, Key=Employment - GET /api/v1/profiles/{profileId}/employment?provider=DGP",
                "Provider=ABC, Key=Security - GET /api/v1/profiles/{profileId}/security?provider=ABC",
                "Provider=ABC, Key=Contacts - GET /api/v1/profiles/{profileId}/contacts?provider=ABC",
                "Provider=DGP, Key=Addresses - GET /api/v1/profiles/{profileId}/addresses?provider=DGP",
                "Provider=ABC, Key=OrgInfo - GET /api/v1/profiles/{profileId}/organization?provider=ABC",
                "Provider=DGP, Key=Submissions - GET /api/v1/profiles/{profileId}/submissions?provider=DGP",
                "Provider=ABC, Key=Payments - GET /api/v1/profiles/{profileId}/payments?provider=ABC"
            }
        }
    };
    
    return Results.Ok(response);
})
.WithName("GetDefaultData")
.WithOpenApi();

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { Status = "Healthy", Timestamp = DateTime.UtcNow }))
   .WithName("HealthCheck")
   .WithOpenApi();

app.Run();
