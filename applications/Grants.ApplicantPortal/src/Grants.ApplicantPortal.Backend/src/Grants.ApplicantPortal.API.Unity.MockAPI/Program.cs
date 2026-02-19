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

// Unity Mock API — single profile data endpoint matching the real Unity contract
// GET /api/app/applicant-profiles/profile?TenantId=X&Key=CONTACTINFO&ProfileId=X&Subject=X
app.MapGet("/api/app/applicant-profiles/profile", (Guid? ProfileId, string? Subject, string? TenantId, string? Key) =>
{
    app.Logger.LogInformation("GetProfile called with ProfileId: {ProfileId}, Subject: {Subject}, TenantId: {TenantId}, Key: {Key}",
        ProfileId, Subject, TenantId, Key);

    if (ProfileId is null || string.IsNullOrEmpty(Subject) || string.IsNullOrEmpty(TenantId) || string.IsNullOrEmpty(Key))
    {
        return Results.BadRequest(new ProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Bad Request",
            Detail = "ProfileId, Subject, TenantId, and Key query parameters are required."
        });
    }

    var data = ResolveProfileData(TenantId, Key.ToUpperInvariant());

    var response = new
    {
        profileId = ProfileId,
        subject = Subject,
        key = Key.ToUpperInvariant(),
        tenantId = TenantId,
        data
    };

    return Results.Ok(response);
})
.WithName("GetProfileData")
.WithOpenApi();

// Tenants (providers) endpoint — returns the available providers for this Unity instance
// Accepts ProfileId and Subject query parameters to match the real Unity API contract
app.MapGet("/api/app/applicant-profiles/tenants", (Guid? ProfileId, string? Subject) =>
{
    app.Logger.LogInformation("GetTenants called with ProfileId: {ProfileId}, Subject: {Subject}",
        ProfileId, Subject);

    if (ProfileId is null || string.IsNullOrEmpty(Subject))
    {
        return Results.BadRequest(new ProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Bad Request",
            Detail = "ProfileId and Subject query parameters are required."
        });
    }

    var tenants = new[]
    {
        new { TenantId = "DGP", TenantName = "DGP" },
        new { TenantId = "ABC", TenantName = "ABC" }
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

// ─── Mock data resolution ───────────────────────────────────────────────────

static object ResolveProfileData(string tenantId, string key) => key switch
{
    "CONTACTINFO" => GetContactInfoData(tenantId),
    "ADDRESSINFO" => GetAddressInfoData(),
    "ORGINFO" => GetOrgInfoData(tenantId),
    "SUBMISSIONINFO" => GetSubmissionInfoData(),
    "PAYMENTINFO" => GetPaymentInfoData(),
    _ => new { message = $"No mock data for key: {key}", supportedKeys = new[] { "CONTACTINFO", "ADDRESSINFO", "ORGINFO", "SUBMISSIONINFO", "PAYMENTINFO" } }
};

static object GetContactInfoData(string tenantId) => tenantId.ToUpperInvariant() switch
{
    "DGP" => new
    {
        dataType = "CONTACTINFO",
        contacts = new object[]
        {
            new { contactId = "a437675a-d642-455c-b3e0-388d75e6203f", name = "Alex Johnson", title = "Unity Program Director", email = "alex.johnson@unity.gov", homePhoneNumber = "555 123-4567", mobilePhoneNumber = "555 987-6543", workPhoneNumber = "555 864-2100", workPhoneExtension = "101", contactType = "ApplicantProfile", role = "Executive", isPrimary = true, isEditable = true, applicationId = (string?)null },
            new { contactId = "b5a01793-e247-48c7-8257-25b0ed239883", name = "Sarah Mitchell", title = "Unity Grants Manager", email = "sarah.mitchell@unity.gov", homePhoneNumber = (string?)null, mobilePhoneNumber = (string?)null, workPhoneNumber = (string?)null, workPhoneExtension = (string?)null, contactType = "Application", role = "Additional Signing Authority", isPrimary = false, isEditable = false, applicationId = "3a1eac9f-da13-a888-883f-d2c0575e7620" }
        }
    },
    _ => new
    {
        dataType = "CONTACTINFO",
        contacts = new object[]
        {
            new { contactId = "c1234567-89ab-cdef-0123-456789abcdef", name = "Dr. Emma Wilson", title = "Research Director", email = "emma.wilson@unity-abc.gov", homePhoneNumber = (string?)null, mobilePhoneNumber = "555 222-3344", workPhoneNumber = "555 864-5500", workPhoneExtension = (string?)null, contactType = "ApplicantProfile", role = "Primary", isPrimary = true, isEditable = true, applicationId = (string?)null },
            new { contactId = "d2345678-9abc-def0-1234-56789abcdef0", name = "Michael Chen", title = "Business Development Manager", email = "michael.chen@unity-abc.gov", homePhoneNumber = (string?)null, mobilePhoneNumber = (string?)null, workPhoneNumber = (string?)null, workPhoneExtension = (string?)null, contactType = "Application", role = "Additional Signing Authority", isPrimary = false, isEditable = false, applicationId = "3a1eac9f-da13-a888-883f-d2c0575e7620" }
        }
    }
};

static object GetAddressInfoData() => new
{
    Addresses = new[]
    {
        new { Id = "AAD12E34-6789-0ABC-DEF1-234567890ABC", AddressId = "ADDR-U-001", Type = "Physical", AddressLine1 = "1234 Government Street", AddressLine2 = "Suite 500", City = "Victoria", Province = "BC", PostalCode = "V8W 1A4", Country = "Canada", IsPrimary = true, IsActive = true, AllowEdit = true, LastVerified = DateTime.UtcNow.AddDays(-30) },
        new { Id = "BBD12E34-6789-0ABC-DEF1-234567890ABC", AddressId = "ADDR-U-002", Type = "Mailing", AddressLine1 = "5678 Unity Drive", AddressLine2 = "", City = "Vancouver", Province = "BC", PostalCode = "V6B 2C3", Country = "Canada", IsPrimary = false, IsActive = true, AllowEdit = true, LastVerified = DateTime.UtcNow.AddDays(-15) }
    },
    Summary = new { TotalAddresses = 2, PrimaryAddressCount = 1, ActiveAddressCount = 2 }
};

static object GetOrgInfoData(string tenantId) => tenantId.ToUpperInvariant() switch
{
    "DGP" => new
    {
        OrganizationInfo = new
        {
            OrgName = "Unity Government Solutions", OrgNumber = "UGS001234", OrgStatus = "Active",
            OrganizationType = "Government Department", NonRegOrgName = "Unity Tech Division",
            OrgSize = "150", FiscalMonth = "Apr", FiscalDay = 1,
            OrganizationId = "7AEF7815-27D3-5E9C-9686-68E6F36C51EA",
            LegalName = "Unity Government Solutions Department", DoingBusinessAs = "UGS",
            LastUpdated = DateTime.UtcNow.AddDays(-3), AllowEdit = true
        }
    },
    _ => new
    {
        OrganizationInfo = new
        {
            OrgName = "Unity ABC Division", OrgNumber = "ABC987654", OrgStatus = "Active",
            OrganizationType = "Research Institute", NonRegOrgName = "Advanced Business Consulting",
            OrgSize = "75", FiscalMonth = "Jan", FiscalDay = 15,
            OrganizationId = "8BCD8926-38E4-6F0D-A797-79F7F47D62FB",
            LegalName = "Unity Advanced Business Consulting Institute", DoingBusinessAs = "UABC",
            LastUpdated = DateTime.UtcNow.AddDays(-1), AllowEdit = true
        }
    }
};

static object GetSubmissionInfoData() => new
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
            LastModified = DateTime.UtcNow.AddDays(-7)
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
            SubmissionDate = DateTime.UtcNow.AddDays(-14),
            LastModified = DateTime.UtcNow.AddDays(-2)
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
            SubmissionDate = DateTime.UtcNow.AddDays(-3),
            LastModified = DateTime.UtcNow.AddDays(-1)
        }
    },
    Summary = new { TotalSubmissions = 3, TotalRequestedAmount = 725000, TotalPaidAmount = 225000, ApprovedCount = 1, UnderReviewCount = 1, SubmittedCount = 1 }
};

static object GetPaymentInfoData() => new
{
    Payments = new[]
    {
        new
        {
            PaymentId = "PAY-UNI-001", SubmissionId = "UNI-SUB-001", ApplicationId = "APP-UNI-2024-001",
            GrantTitle = "Digital Government Transformation", AwardAmount = 350000,
            PaymentMethod = "Government Transfer", BankAccount = "****-****-****-7890"
        }
    },
    PaymentSummary = new { TotalAwardAmount = 350000, TotalPaid = 175000, TotalPending = 175000 }
};
