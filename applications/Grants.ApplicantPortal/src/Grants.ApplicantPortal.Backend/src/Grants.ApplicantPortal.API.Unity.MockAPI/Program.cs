using Microsoft.AspNetCore.Mvc;
using Grants.ApplicantPortal.API.Unity.MockAPI;

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

// Register RabbitMQ consumer that listens for commands from the Grants Portal outbox
// and sends acknowledgments back via RabbitMQ (consumed by the Portal inbox)
builder.Services.AddHostedService<UnityCommandConsumerService>();

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

// Messaging status endpoint — shows the RabbitMQ configuration used by this mock
app.MapGet("/messaging/status", (IConfiguration config) =>
{
    var host = config["RabbitMQ:HostName"] ?? "localhost";
    var port = config.GetValue("RabbitMQ:Port", 5672);
    var queue = config["RabbitMQ:InboundQueue"] ?? "unity.mockapi.commands";
    var exchange = config["RabbitMQ:Exchange"] ?? "grants.messaging";
    var routingKeys = config.GetSection("RabbitMQ:InboundRoutingKeys").Get<string[]>() ?? ["grants.unity.#"];
    var ackRoutingKey = config["RabbitMQ:AckRoutingKey"] ?? "grants.unity.acknowledgment";

    return Results.Ok(new
    {
        RabbitMQ = new { Host = host, Port = port },
        Consumer = new { Queue = queue, Exchange = exchange, RoutingKeys = routingKeys },
        Publisher = new { AckRoutingKey = ackRoutingKey },
        Timestamp = DateTime.UtcNow
    });
})
.WithName("MessagingStatus")
.WithOpenApi();

app.Run();

// ─── Mock data resolution ───────────────────────────────────────────────────

static object ResolveProfileData(string tenantId, string key) => key switch
{
    "CONTACTINFO" => GetContactInfoData(tenantId),
    "ADDRESSINFO" => GetAddressInfoData(tenantId),
    "ORGINFO" => GetOrgInfoData(tenantId),
    "SUBMISSIONINFO" => GetSubmissionInfoData(tenantId),
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

static object GetAddressInfoData(string tenantId) => tenantId.ToUpperInvariant() switch
{
    "DGP" => new
    {
        dataType = "ADDRESSINFO",
        addresses = new[]
        {
            new { id = "AAD12E34-6789-0ABC-DEF1-234567890ABC", addressType = "Physical", street = "1234 Government Street", street2 = "Suite 500", unit = "", city = "Victoria", province = "BC", postalCode = "V8W1A4", country = "", isPrimary = true, isEditable = false, referenceNo = "D59AA865" },
            new { id = "BBD12E34-6789-0ABC-DEF1-234567890ABC", addressType = "Mailing", street = "5678 Unity Drive", street2 = "", unit = "", city = "Vancouver", province = "BC", postalCode = "V6B2C3", country = "", isPrimary = false, isEditable = false, referenceNo = "D59AA865" }
        }
    },
    _ => new
    {
        dataType = "ADDRESSINFO",
        addresses = new[]
        {
            new { id = "CCD12E34-6789-0ABC-DEF1-234567890ABC", addressType = "Physical", street = "900 Research Parkway", street2 = "Building C", unit = "", city = "Kelowna", province = "BC", postalCode = "V1Y8K2", country = "", isPrimary = true, isEditable = false, referenceNo = "E68BB976" }
        }
    }
};

static object GetOrgInfoData(string tenantId) => tenantId.ToUpperInvariant() switch
{
    "DGP" => new
    {
        dataType = "ORGINFO",
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
        dataType = "ORGINFO",
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

static object GetSubmissionInfoData(string tenantId) => tenantId.ToUpperInvariant() switch
{
    "DGP" => new
    {
        dataType = "SUBMISSIONINFO",
        submissions = new[]
        {
            new
            {
                id = "3a1ed607-d049-b152-c059-40288fe412d9",
                linkId = "d59aa865-9d15-45e6-bb07-6b963ce3c9de",
                receivedTime = DateTime.UtcNow.AddDays(-30).ToString("o"),
                submissionTime = DateTime.UtcNow.AddDays(-30).AddMinutes(-9).ToString("o"),
                referenceNo = "D59AA865",
                projectName = "Digital Government Transformation",
                status = "Submitted"
            },
            new
            {
                id = "3a1eac9f-dcb4-2df4-4faa-11179f15e872",
                linkId = "1faccaa3-69c6-4d88-9ed1-73a953b8e417",
                receivedTime = DateTime.UtcNow.AddDays(-14).ToString("o"),
                submissionTime = DateTime.UtcNow.AddDays(-14).AddMinutes(-55).ToString("o"),
                referenceNo = "1FACCAA3",
                projectName = "Cybersecurity Enhancement Program",
                status = "Under Review"
            },
            new
            {
                id = "3a1f420f-091f-b547-de4c-9e153795e21b",
                linkId = "1987843f-7c56-4022-8fa1-8f5c1b7449cb",
                receivedTime = DateTime.UtcNow.AddDays(-3).ToString("o"),
                submissionTime = DateTime.UtcNow.AddDays(-3).AddMinutes(-40).ToString("o"),
                referenceNo = "1987843F",
                projectName = "Citizen Services Portal",
                status = "Submitted"
            }
        },
        linkSource = "https://chefs-dev.apps.silver.devops.gov.bc.ca/app/user/view?s="
    },
    _ => new
    {
        dataType = "SUBMISSIONINFO",
        submissions = new[]
        {
            new
            {
                id = "4b2fe718-e15a-c263-d16a-51399gf523ea",
                linkId = "e68bb976-a026-56f7-cc18-7c074df4dafe",
                receivedTime = DateTime.UtcNow.AddDays(-7).ToString("o"),
                submissionTime = DateTime.UtcNow.AddDays(-7).AddMinutes(-12).ToString("o"),
                referenceNo = "E68BB976",
                projectName = "Advanced Research Initiative",
                status = "Under Review"
            },
            new
            {
                id = "5c3gf829-f26b-d374-e27b-62410hg634fb",
                linkId = "f79cc087-b137-67g8-dd29-8d185eg5ebgf",
                receivedTime = DateTime.UtcNow.AddDays(-21).ToString("o"),
                submissionTime = DateTime.UtcNow.AddDays(-21).AddMinutes(-33).ToString("o"),
                referenceNo = "F79CC087",
                projectName = "Community Outreach Program",
                status = "Submitted"
            }
        },
        linkSource = "https://chefs-dev.apps.silver.devops.gov.bc.ca/app/user/view?s="
    }
};

static object GetPaymentInfoData() => new
{
    dataType = "PAYMENTINFO",
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
