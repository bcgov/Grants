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

// API Key validation middleware — validates X-Api-Key header on all /api/ routes
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/api"))
    {
        var expectedApiKey = context.RequestServices
            .GetRequiredService<IConfiguration>()
            .GetValue<string>("ApiKey");

        if (!context.Request.Headers.TryGetValue("X-Api-Key", out var providedKey) ||
            !string.Equals(providedKey, expectedApiKey, StringComparison.Ordinal))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Unauthorized",
                Detail = "Invalid API Key",
                Type = "https://tools.ietf.org/html/rfc7235#section-3.1"
            });
            return;
        }
    }

    await next();
});

// Unity Mock API Endpoints
app.MapGet("/api/profiles/{profileId}", (Guid profileId, string? provider = null) =>
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

app.MapGet("/api/profiles/{profileId}/employment", (Guid profileId, string? provider = null) =>
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

app.MapGet("/api/profiles/{profileId}/security", (Guid profileId, string? provider = null) =>
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

app.MapGet("/api/profiles/{profileId}/contacts", (Guid profileId, string? provider = null) =>
{
    var actualProvider = provider ?? "DGP"; // Default to DGP if not specified
    
    var contactsData = actualProvider.ToUpper() switch
    {
        "DGP" => new
        {
            Contacts = new[]
            {
                new
                {
                    Id = "A437675A-D642-455C-B3E0-388D75E6203F",
                    Type = "Primary",
                    Name = "Alex Johnson",
                    Email = "alex.johnson@unity.gov",
                    Phone = "+1-555-UNITY-1",
                    Title = "Unity Program Director",
                    IsPrimary = true,
                    IsActive = true,
                    LastUpdated = DateTime.UtcNow.AddDays(-5),
                    AllowEdit = true
                },
                new
                {
                    Id = "B5A01793-E247-48C7-8257-25B0ED239883",
                    Type = "Secondary", 
                    Name = "Sarah Mitchell",
                    Email = "sarah.mitchell@unity.gov",
                    Phone = "+1-555-UNITY-2",
                    Title = "Unity Grants Manager",
                    IsPrimary = false,
                    IsActive = true,
                    LastUpdated = DateTime.UtcNow.AddDays(-2),
                    AllowEdit = true
                }
            },
            Summary = new
            {
                TotalContacts = 2,
                PrimaryContactCount = 1,
                ActiveContactCount = 2
            }
        },
        "ABC" => new
        {
            Contacts = new[]
            {
                new
                {
                    Id = "C1234567-89AB-CDEF-0123-456789ABCDEF",
                    Type = "Primary",
                    Name = "Dr. Emma Wilson",
                    Email = "emma.wilson@unity-abc.gov",
                    Phone = "+1-555-ABC-001",
                    Title = "Research Director",
                    IsPrimary = true,
                    IsActive = true,
                    LastUpdated = DateTime.UtcNow.AddDays(-3),
                    AllowEdit = true
                },
                new
                {
                    Id = "D2345678-9ABC-DEF0-1234-56789ABCDEF0",
                    Type = "Secondary",
                    Name = "Michael Chen",
                    Email = "michael.chen@unity-abc.gov", 
                    Phone = "+1-555-ABC-002",
                    Title = "Business Development Manager",
                    IsPrimary = false,
                    IsActive = true,
                    LastUpdated = DateTime.UtcNow.AddDays(-1),
                    AllowEdit = true
                }
            },
            Summary = new
            {
                TotalContacts = 2,
                PrimaryContactCount = 1,
                ActiveContactCount = 2
            }
        },
        _ => throw new ArgumentException($"Unsupported provider: {actualProvider}")
    };

    // Convert to JSON string to match DEMO plugin behavior
    var jsonString = System.Text.Json.JsonSerializer.Serialize(contactsData, new System.Text.Json.JsonSerializerOptions
    {
        PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
    });

    var response = new
    {
        profileId = profileId,
        pluginId = "UNITY",
        provider = actualProvider,
        data = jsonString,
        populatedAt = DateTime.UtcNow
    };
    
    return Results.Ok(response);
})
.WithName("GetContacts")
.WithOpenApi();

app.MapGet("/api/profiles/{profileId}/addresses", (Guid profileId, string? provider = null) =>
{
    var actualProvider = provider ?? "DGP"; // Default to DGP if not specified
    
    var addressesData = new
    {
        Addresses = new[]
        {
            new
            {
                Id = "AAD12E34-6789-0ABC-DEF1-234567890ABC",
                AddressId = "ADDR-U-001",
                Type = "Physical",
                AddressLine1 = "1234 Government Street",
                AddressLine2 = "Suite 500",
                Street = "1234 Government Street, Suite 500",
                City = "Victoria",
                Province = "BC",
                PostalCode = "V8W 1A4",
                Country = "Canada",
                IsPrimary = true,
                IsActive = true,
                LastVerified = DateTime.UtcNow.AddDays(-10),
                AllowEdit = true
            },
            new
            {
                Id = "BBD12E34-6789-0ABC-DEF1-234567890ABC",
                AddressId = "ADDR-U-002",
                Type = "Mailing",
                AddressLine1 = "5678 Unity Drive",
                AddressLine2 = "",
                Street = "5678 Unity Drive",
                City = "Vancouver",
                Province = "BC",
                PostalCode = "V6B 2C3",
                Country = "Canada",
                IsPrimary = false,
                IsActive = true,
                LastVerified = DateTime.UtcNow.AddDays(-7),
                AllowEdit = true
            }
        },
        Summary = new
        {
            TotalAddresses = 2,
            PrimaryAddressCount = 1,
            ActiveAddressCount = 2
        }
    };

    // Convert to JSON string to match DEMO plugin behavior
    var jsonString = System.Text.Json.JsonSerializer.Serialize(addressesData, new System.Text.Json.JsonSerializerOptions
    {
        PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
    });

    var response = new
    {
        profileId = profileId,
        pluginId = "UNITY",
        provider = actualProvider,
        data = jsonString,
        populatedAt = DateTime.UtcNow
    };
    
    return Results.Ok(response);
})
.WithName("GetAddresses")
.WithOpenApi();

app.MapGet("/api/profiles/{profileId}/organization", (Guid profileId, string? provider = null) =>
{
    var actualProvider = provider ?? "DGP"; // Default to DGP if not specified
    
    var organizationData = actualProvider.ToUpper() switch
    {
        "DGP" => new
        {
            OrganizationInfo = new
            {
                OrgName = "Unity Government Solutions",
                OrgNumber = "UGS001234",
                OrgStatus = "Active",
                OrganizationType = "Government Department",
                NonRegOrgName = "Unity Tech Division",
                OrgSize = "150",
                FiscalMonth = "Apr",
                FiscalDay = 1,
                OrganizationId = "7AEF7815-27D3-5E9C-9686-68E6F36C51EA",
                LegalName = "Unity Government Solutions Department",
                DoingBusinessAs = "UGS",
                EIN = "12-3456789",
                Founded = 1985,
                Address = new
                {
                    Street = "1234 Government Street",
                    City = "Victoria",
                    State = "BC",
                    ZipCode = "V8W 1A4",
                    Country = "Canada"
                },
                ContactInfo = new
                {
                    PrimaryContact = new
                    {
                        Name = "Alex Johnson",
                        Title = "Unity Program Director",
                        Email = "alex.johnson@unity.gov",
                        Phone = "+1-555-UNITY-1"
                    },
                    GrantsContact = new
                    {
                        Name = "Sarah Mitchell",
                        Title = "Unity Grants Manager", 
                        Email = "sarah.mitchell@unity.gov",
                        Phone = "+1-555-UNITY-2"
                    }
                },
                Mission = "To provide innovative government solutions and support efficient public service delivery through technology and collaboration.",
                ServicesAreas = new[] { "Government Services", "Technology Solutions", "Public Administration", "Digital Services" },
                Certifications = new[]
                {
                    new { Type = "Government Security Clearance", ValidUntil = DateTime.UtcNow.AddYears(3) },
                    new { Type = "ISO 27001 Certification", ValidUntil = DateTime.UtcNow.AddYears(2) }
                },
                UnitySpecific = new
                {
                    EligibilityStatus = "Verified",
                    LastAuditDate = DateTime.UtcNow.AddMonths(-4),
                    ComplianceScore = 98,
                    SpecialDesignations = new[] { "Digital Government Hub", "Innovation Center" },
                    SecurityClearance = "Top Secret",
                    DepartmentCode = "UNI-GOV-001"
                },
                LastUpdated = DateTime.UtcNow.AddDays(-3),
                AllowEdit = true
            }
        },
        "ABC" => new
        {
            OrganizationInfo = new
            {
                OrgName = "Unity ABC Division",
                OrgNumber = "ABC987654",
                OrgStatus = "Active",
                OrganizationType = "Research Institute",
                NonRegOrgName = "Advanced Business Consulting",
                OrgSize = "75",
                FiscalMonth = "Jan",
                FiscalDay = 15,
                OrganizationId = "8BCD8926-38E4-6F0D-A797-79F7F47D62FB",
                LegalName = "Unity Advanced Business Consulting Institute",
                DoingBusinessAs = "UABC",
                EIN = "98-7654321",
                Founded = 1995,
                Address = new
                {
                    Street = "5678 Research Boulevard",
                    City = "Vancouver",
                    State = "BC",
                    ZipCode = "V6T 1Z4",
                    Country = "Canada"
                },
                ContactInfo = new
                {
                    PrimaryContact = new
                    {
                        Name = "Dr. Emma Wilson",
                        Title = "Research Director",
                        Email = "emma.wilson@unity-abc.gov",
                        Phone = "+1-555-ABC-001"
                    },
                    GrantsContact = new
                    {
                        Name = "Michael Chen",
                        Title = "Business Development Manager",
                        Email = "michael.chen@unity-abc.gov",
                        Phone = "+1-555-ABC-002"
                    }
                },
                Mission = "To advance business consulting and research initiatives that drive innovation in government operations.",
                ServicesAreas = new[] { "Business Analysis", "Research & Development", "Strategic Consulting", "Data Analytics" },
                Certifications = new[]
                {
                    new { Type = "Research Excellence Certification", ValidUntil = DateTime.UtcNow.AddYears(2) },
                    new { Type = "Business Consulting License", ValidUntil = DateTime.UtcNow.AddYears(1) }
                },
                UnitySpecific = new
                {
                    EligibilityStatus = "Verified",
                    LastAuditDate = DateTime.UtcNow.AddMonths(-2),
                    ComplianceScore = 94,
                    SpecialDesignations = new[] { "Research Excellence Center", "Business Innovation Hub" },
                    SecurityClearance = "Confidential",
                    DepartmentCode = "UNI-ABC-002"
                },
                LastUpdated = DateTime.UtcNow.AddDays(-1),
                AllowEdit = true
            }
        },
        _ => throw new ArgumentException($"Unsupported provider: {actualProvider}")
    };

    // Convert to JSON string to match DEMO plugin behavior
    var jsonString = System.Text.Json.JsonSerializer.Serialize(organizationData, new System.Text.Json.JsonSerializerOptions
    {
        PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
    });

    var response = new
    {
        profileId = profileId,
        pluginId = "UNITY",
        provider = actualProvider,
        data = jsonString,
        populatedAt = DateTime.UtcNow
    };
    
    return Results.Ok(response);
})
.WithName("GetOrganization")
.WithOpenApi();

app.MapGet("/api/profiles/{profileId}/submissions", (Guid profileId, string? provider = null) =>
{
    var actualProvider = provider ?? "DGP"; // Default to DGP if not specified
    
    var submissionsData = new
    {
        Submissions = new[]
        {
            new
            {
                Id = "A1234E56-789A-BC01-23DE-F4567890AB12",
                SubmissionId = "UNI-SUB-001",
                ApplicationId = "APP-UNI-2024-001",
                ProjectName = "Digital Government Transformation",
                ProgramName = "Unity - Digital Innovation",
                RequestedAmount = 350000,
                PaidAmount = 175000,
                Status = "Approved",
                StatusCode = "GRANT_APPROVED",
                SubmissionDate = DateTime.UtcNow.AddDays(-30),
                LastModified = DateTime.UtcNow.AddDays(-5),
                ProjectPeriod = new
                {
                    StartDate = DateTime.UtcNow.AddMonths(1),
                    EndDate = DateTime.UtcNow.AddMonths(18)
                }
            },
            new
            {
                Id = "A2345E67-890A-BC12-34DE-F5678901AB23",
                SubmissionId = "UNI-SUB-002",
                ApplicationId = "APP-UNI-2024-002",
                ProjectName = "Cybersecurity Enhancement Program",
                ProgramName = "Unity - Security Solutions",
                RequestedAmount = 200000,
                PaidAmount = 50000,
                Status = "Under Review",
                StatusCode = "UNDER_INITIAL_REVIEW",
                SubmissionDate = DateTime.UtcNow.AddDays(-20),
                LastModified = DateTime.UtcNow.AddDays(-2),
                ProjectPeriod = new
                {
                    StartDate = DateTime.UtcNow.AddMonths(2),
                    EndDate = DateTime.UtcNow.AddMonths(14)
                }
            },
            new
            {
                Id = "A3456E78-901A-BC23-45DE-F6789012AB34",
                SubmissionId = "UNI-SUB-003",
                ApplicationId = "APP-UNI-2024-003",
                ProjectName = "Citizen Services Portal",
                ProgramName = "Unity - Public Services",
                RequestedAmount = 175000,
                PaidAmount = 0,
                Status = "Submitted",
                StatusCode = "SUBMITTED",
                SubmissionDate = DateTime.UtcNow.AddDays(-10),
                LastModified = DateTime.UtcNow.AddDays(-1),
                ProjectPeriod = new
                {
                    StartDate = DateTime.UtcNow.AddMonths(3),
                    EndDate = DateTime.UtcNow.AddMonths(15)
                }
            }
        },
        Summary = new
        {
            TotalSubmissions = 3,
            TotalRequestedAmount = 725000,
            TotalPaidAmount = 225000,
            ApprovedCount = 1,
            UnderReviewCount = 1,
            SubmittedCount = 1
        }
    };

    // Convert to JSON string to match DEMO plugin behavior
    var jsonString = System.Text.Json.JsonSerializer.Serialize(submissionsData, new System.Text.Json.JsonSerializerOptions
    {
        PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
    });

    var response = new
    {
        profileId = profileId,
        pluginId = "UNITY",
        provider = actualProvider,
        data = jsonString,
        populatedAt = DateTime.UtcNow
    };
    
    return Results.Ok(response);
})
.WithName("GetSubmissions")
.WithOpenApi();

app.MapGet("/api/profiles/{profileId}/payments", (Guid profileId, string? provider = null) =>
{
    var actualProvider = provider ?? "DGP"; // Default to DGP if not specified
    
    var paymentsData = new
    {
        Payments = new[]
        {
            new
            {
                PaymentId = "PAY-UNI-001",
                SubmissionId = "UNI-SUB-001",
                ApplicationId = "APP-UNI-2024-001",
                GrantTitle = "Digital Government Transformation",
                AwardAmount = 350000,
                PaymentSchedule = new[]
                {
                    new
                    {
                        PaymentNumber = 1,
                        Amount = 175000,
                        DueDate = DateTime.UtcNow.AddMonths(1),
                        Status = "Completed",
                        Description = "Initial funding - 50%"
                    },
                    new
                    {
                        PaymentNumber = 2,
                        Amount = 175000,
                        DueDate = DateTime.UtcNow.AddMonths(9),
                        Status = "Scheduled",
                        Description = "Final payment - 50%"
                    }
                },
                PaymentMethod = "Government Transfer",
                BankAccount = "****-****-****-7890",
                TaxReporting = new
                {
                    TaxYear = DateTime.UtcNow.Year,
                    Form1099Required = false,
                    ReportingStatus = "Government Entity"
                }
            }
        },
        PaymentSummary = new
        {
            TotalAwardAmount = 350000,
            TotalPaid = 175000,
            TotalPending = 175000,
            NextPaymentDue = DateTime.UtcNow.AddMonths(9),
            NextPaymentAmount = 175000
        }
    };

    // Convert to JSON string to match DEMO plugin behavior
    var jsonString = System.Text.Json.JsonSerializer.Serialize(paymentsData, new System.Text.Json.JsonSerializerOptions
    {
        PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
    });

    var response = new
    {
        profileId = profileId,
        pluginId = "UNITY",
        provider = actualProvider,
        data = jsonString,
        populatedAt = DateTime.UtcNow
    };
    
    return Results.Ok(response);
})
.WithName("GetPayments")
.WithOpenApi();

app.MapGet("/api/profiles/{profileId}/data", (Guid profileId, string? provider = null) =>
{
    var actualProvider = provider ?? "DGP"; // Default to DGP if not specified
    
    var defaultData = new
    {
        Message = "Unity Mock API - Data available for providers and keys",
        AvailableProviders = new[] { "DGP", "ABC" },
        AvailableKeys = new[] { "CONTACTS", "ADDRESSES", "ORGINFO", "SUBMISSIONS", "PAYMENTS" },
        Instructions = "Use specific endpoints to get Unity data that matches DEMO plugin structure",
        Endpoints = new[]
        {
            $"GET /api/profiles/{profileId}/contacts?provider={actualProvider} - Contact information",
            $"GET /api/profiles/{profileId}/addresses?provider={actualProvider} - Address information", 
            $"GET /api/profiles/{profileId}/organization?provider={actualProvider} - Organization information",
            $"GET /api/profiles/{profileId}/submissions?provider={actualProvider} - Submission history",
            $"GET /api/profiles/{profileId}/payments?provider={actualProvider} - Payment information"
        },
        DataFormat = "Response format matches DEMO plugin structure with profileId, pluginId, provider, data (JSON string), and populatedAt"
    };

    // Convert to JSON string to match DEMO plugin behavior
    var jsonString = System.Text.Json.JsonSerializer.Serialize(defaultData, new System.Text.Json.JsonSerializerOptions
    {
        PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
    });

    var response = new
    {
        profileId = profileId,
        pluginId = "UNITY",
        provider = actualProvider,
        data = jsonString,
        populatedAt = DateTime.UtcNow
    };
    
    return Results.Ok(response);
})
.WithName("GetDefaultData")
.WithOpenApi();

// Tenants (providers) endpoint — returns the available providers for this Unity instance
app.MapGet("/api/app/applicant-profiles/tenants", () =>
{
    var tenants = new[]
    {
        new { Id = "DGP", Name = "DGP" },
        new { Id = "ABC", Name = "ABC" }
    };

    return Results.Ok(tenants);
})
.WithName("GetTenants")
.WithOpenApi();

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { Status = "Healthy", Timestamp = DateTime.UtcNow }))
   .WithName("HealthCheck")
   .WithOpenApi();

app.Run();
