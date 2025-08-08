using Grants.ApplicantPortal.API.Core.Plugins;
using System.Text.Json;

namespace Grants.ApplicantPortal.API.Infrastructure.Plugins.Demo;

/// <summary>
/// Demo profile plugin for testing and demonstration purposes
/// </summary>
public class DemoProfilePlugin(ILogger<DemoProfilePlugin> logger) : IProfilePlugin
{
    public string PluginId => "DEMO";

    public bool CanHandle(ProfilePopulationMetadata metadata)
    {
        return metadata.PluginId.Equals(PluginId, StringComparison.OrdinalIgnoreCase);
    }

    public async Task<ProfileData> PopulateProfileAsync(ProfilePopulationMetadata metadata, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Demo plugin populating profile for ProfileId: {ProfileId}, Provider: {Provider}, Key: {Key}", 
            metadata.ProfileId, metadata.Provider, metadata.Key);

        try
        {
            // Simulate some processing time
            await Task.Delay(50, cancellationToken);

            var mockProfileData = GenerateMockData(metadata);

            var jsonData = JsonSerializer.Serialize(mockProfileData, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            });

            logger.LogInformation("Demo plugin successfully populated profile for ProfileId: {ProfileId}, Provider: {Provider}, Key: {Key}", 
                metadata.ProfileId, metadata.Provider, metadata.Key);

            return new ProfileData(
                metadata.ProfileId,
                metadata.PluginId,
                metadata.Provider,
                metadata.Key,
                jsonData);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Demo plugin failed to populate profile for ProfileId: {ProfileId}, Provider: {Provider}, Key: {Key}", 
                metadata.ProfileId, metadata.Provider, metadata.Key);
            throw;
        }
    }

    private object GenerateMockData(ProfilePopulationMetadata metadata)
    {
        var baseData = new
        {
            ProfileId = metadata.ProfileId,
            Provider = metadata.Provider,
            Key = metadata.Key,
            Source = "Demo System",
            PopulatedAt = DateTime.UtcNow,
            PopulatedBy = PluginId
        };

        return (metadata.Provider?.ToUpper(), metadata.Key?.ToUpper()) switch
        {
            ("DEMO", "SUBMISSIONS") => GenerateProgram1Submissions(baseData),
            ("DEMO", "ORGINFO") => GenerateProgram1OrgInfo(baseData),
            ("DEMO", "PAYMENTS") => GenerateProgram1Payments(baseData),
            ("UNITY", "SUBMISSIONS") => GenerateProgram2Submissions(baseData),
            ("UNITY", "ORGINFO") => GenerateProgram2OrgInfo(baseData),
            ("UNITY", "PAYMENTS") => GenerateProgram2Payments(baseData),
            _ => GenerateDefaultData(baseData)
        };
    }

    private object GenerateProgram1Submissions(object baseData)
    {
        return new
        {
            baseData,
            Data = new
            {
                Submissions = new[]
                {
                    new
                    {
                        SubmissionId = "PROG1-SUB-001",
                        ApplicationId = "APP-2024-0001",
                        Title = "Community Health Initiative",
                        ProgramName = "Program1 - Health & Wellness",
                        RequestedAmount = 150000,
                        Status = "Under Review",
                        SubmittedDate = DateTime.UtcNow.AddDays(-15),
                        LastModified = DateTime.UtcNow.AddDays(-2),
                        ProjectPeriod = new
                        {
                            StartDate = DateTime.UtcNow.AddMonths(2),
                            EndDate = DateTime.UtcNow.AddMonths(14)
                        },
                        Categories = new[] { "Healthcare", "Community Outreach", "Prevention" }
                    },
                    new
                    {
                        SubmissionId = "PROG1-SUB-002",
                        ApplicationId = "APP-2024-0045",
                        Title = "Youth Mental Health Support Program",
                        ProgramName = "Program1 - Health & Wellness",
                        RequestedAmount = 85000,
                        Status = "Approved",
                        SubmittedDate = DateTime.UtcNow.AddDays(-45),
                        LastModified = DateTime.UtcNow.AddDays(-10),
                        ProjectPeriod = new
                        {
                            StartDate = DateTime.UtcNow.AddMonths(1),
                            EndDate = DateTime.UtcNow.AddMonths(13)
                        },
                        Categories = new[] { "Mental Health", "Youth Services", "Community Support" }
                    }
                },
                Summary = new
                {
                    TotalSubmissions = 2,
                    TotalRequestedAmount = 235000,
                    ApprovedCount = 1,
                    UnderReviewCount = 1
                }
            }
        };
    }

    private object GenerateProgram1OrgInfo(object baseData)
    {
        return new
        {
            baseData,
            Data = new
            {
                OrganizationInfo = new
                {
                    OrgName = "Cowichan Exhibition",
                    OrgNumber = "S0003748",
                    OrgStatus =  "Active", 
                    OrganizationType = "Society",
                    NonRegOrgName = "Shrine Org",
                    OrgSize = "50",
                    FiscalMonth = "Aug",
                    FiscalDay = 1,
                    OrganizationId = "ORG-DEMO-001",
                    LegalName = "Demo Community Health Foundation",
                    DoingBusinessAs = "DCHF",
                    EIN = "12-3456789",
                    Founded = 2010,
                    Address = new
                    {
                        Street = "123 Health Avenue",
                        City = "Wellness City",
                        State = "CA",
                        ZipCode = "90210",
                        Country = "USA"
                    },
                    ContactInfo = new
                    {
                        PrimaryContact = new
                        {
                            Name = "Dr. Sarah Johnson",
                            Title = "Executive Director",
                            Email = "sarah.johnson@dchf.org",
                            Phone = "+1-555-HEALTH"
                        },
                        GrantsContact = new
                        {
                            Name = "Michael Chen",
                            Title = "Grants Manager",
                            Email = "michael.chen@dchf.org",
                            Phone = "+1-555-GRANTS"
                        }
                    },
                    Mission = "To improve community health outcomes through innovative programs and partnerships.",
                    ServicesAreas = new[] { "Healthcare", "Community Wellness", "Health Education", "Prevention Programs" },
                    Certifications = new[]
                    {
                        new { Type = "CARF Accreditation", ValidUntil = DateTime.UtcNow.AddYears(2) },
                        new { Type = "State Health Department License", ValidUntil = DateTime.UtcNow.AddYears(1) }
                    },
                    Program1Specific = new
                    {
                        EligibilityStatus = "Verified",
                        LastAuditDate = DateTime.UtcNow.AddMonths(-6),
                        ComplianceScore = 95,
                        SpecialDesignations = new[] { "Rural Health Clinic", "FQHC Look-Alike" }
                    }
                }
            }
        };
    }

    private object GenerateProgram1Payments(object baseData)
    {
        return new
        {
            baseData,
            Data = new
            {
                Payments = new[]
                {
                    new
                    {
                        PaymentId = "PAY-PROG1-001",
                        SubmissionId = "PROG1-SUB-002",
                        ApplicationId = "APP-2024-0045",
                        GrantTitle = "Youth Mental Health Support Program",
                        AwardAmount = 85000,
                        PaymentSchedule = new[]
                        {
                            new
                            {
                                PaymentNumber = 1,
                                Amount = 25500,
                                DueDate = DateTime.UtcNow.AddMonths(1),
                                Status = "Scheduled",
                                Description = "Initial funding - 30%"
                            },
                            new
                            {
                                PaymentNumber = 2,
                                Amount = 29750,
                                DueDate = DateTime.UtcNow.AddMonths(6),
                                Status = "Pending",
                                Description = "Mid-term payment - 35%"
                            },
                            new
                            {
                                PaymentNumber = 3,
                                Amount = 29750,
                                DueDate = DateTime.UtcNow.AddMonths(12),
                                Status = "Pending",
                                Description = "Final payment - 35%"
                            }
                        },
                        PaymentMethod = "Electronic Transfer",
                        BankAccount = "****-****-****-5678",
                        TaxReporting = new
                        {
                            TaxYear = DateTime.UtcNow.Year,
                            Form1099Required = true,
                            ReportingStatus = "Pending"
                        }
                    }
                },
                PaymentSummary = new
                {
                    TotalAwardAmount = 85000,
                    TotalPaid = 0,
                    TotalPending = 85000,
                    NextPaymentDue = DateTime.UtcNow.AddMonths(1),
                    NextPaymentAmount = 25500
                }
            }
        };
    }

    private object GenerateProgram2Submissions(object baseData)
    {
        return new
        {
            baseData,
            Data = new
            {
                Submissions = new[]
                {
                    new
                    {
                        SubmissionId = "PROG2-SUB-001",
                        ApplicationId = "APP-2024-0078",
                        Title = "STEM Education Excellence Initiative",
                        ProgramName = "Program2 - Education & Technology",
                        RequestedAmount = 275000,
                        Status = "Approved",
                        SubmittedDate = DateTime.UtcNow.AddDays(-30),
                        LastModified = DateTime.UtcNow.AddDays(-5),
                        ProjectPeriod = new
                        {
                            StartDate = DateTime.UtcNow.AddMonths(1),
                            EndDate = DateTime.UtcNow.AddMonths(25)
                        },
                        Categories = new[] { "Education", "STEM", "Technology", "K-12" }
                    },
                    new
                    {
                        SubmissionId = "PROG2-SUB-002",
                        ApplicationId = "APP-2024-0089",
                        Title = "Digital Literacy for Seniors",
                        ProgramName = "Program2 - Education & Technology",
                        RequestedAmount = 120000,
                        Status = "Under Review",
                        SubmittedDate = DateTime.UtcNow.AddDays(-20),
                        LastModified = DateTime.UtcNow.AddDays(-1),
                        ProjectPeriod = new
                        {
                            StartDate = DateTime.UtcNow.AddMonths(3),
                            EndDate = DateTime.UtcNow.AddMonths(15)
                        },
                        Categories = new[] { "Digital Literacy", "Senior Services", "Community Education" }
                    },
                    new
                    {
                        SubmissionId = "PROG2-SUB-003",
                        ApplicationId = "APP-2024-0095",
                        Title = "Rural Broadband Access Project",
                        ProgramName = "Program2 - Education & Technology",
                        RequestedAmount = 450000,
                        Status = "In Review",
                        SubmittedDate = DateTime.UtcNow.AddDays(-10),
                        LastModified = DateTime.UtcNow.AddDays(-3),
                        ProjectPeriod = new
                        {
                            StartDate = DateTime.UtcNow.AddMonths(4),
                            EndDate = DateTime.UtcNow.AddMonths(28)
                        },
                        Categories = new[] { "Infrastructure", "Rural Development", "Technology Access" }
                    }
                },
                Summary = new
                {
                    TotalSubmissions = 3,
                    TotalRequestedAmount = 845000,
                    ApprovedCount = 1,
                    UnderReviewCount = 2
                }
            }
        };
    }

    private object GenerateProgram2OrgInfo(object baseData)
    {
        return new
        {
            baseData,
            Data = new
            {
                OrganizationInfo = new
                {
                    OrganizationId = "ORG-DEMO-002",
                    LegalName = "Demo Educational Technology Consortium",
                    DoingBusinessAs = "DETC",
                    EIN = "98-7654321",
                    OrganizationType = "Educational Nonprofit",
                    Founded = 2015,
                    Address = new
                    {
                        Street = "456 Innovation Drive",
                        City = "Tech Valley",
                        State = "TX",
                        ZipCode = "75001",
                        Country = "USA"
                    },
                    ContactInfo = new
                    {
                        PrimaryContact = new
                        {
                            Name = "Dr. Maria Rodriguez",
                            Title = "Chief Executive Officer",
                            Email = "maria.rodriguez@detc.edu",
                            Phone = "+1-555-TECH-ED"
                        },
                        GrantsContact = new
                        {
                            Name = "James Liu",
                            Title = "Director of Development",
                            Email = "james.liu@detc.edu",
                            Phone = "+1-555-DEV-FUND"
                        }
                    },
                    Mission = "To bridge the digital divide through innovative educational technology solutions and comprehensive training programs.",
                    ServicesAreas = new[] { "Educational Technology", "Digital Literacy", "STEM Education", "Teacher Training" },
                    Certifications = new[]
                    {
                        new { Type = "Department of Education Partnership", ValidUntil = DateTime.UtcNow.AddYears(3) },
                        new { Type = "Technology Integration Certification", ValidUntil = DateTime.UtcNow.AddYears(2) }
                    },
                    Program2Specific = new
                    {
                        EligibilityStatus = "Verified",
                        LastTechAudit = DateTime.UtcNow.AddMonths(-3),
                        InnovationScore = 88,
                        SpecialDesignations = new[] { "STEM Education Hub", "Rural Technology Center" },
                        Partnerships = new[] { "State University System", "Tech Industry Coalition", "Rural Education Network" }
                    }
                }
            }
        };
    }

    private object GenerateProgram2Payments(object baseData)
    {
        return new
        {
            baseData,
            Data = new
            {
                Payments = new[]
                {
                    new
                    {
                        PaymentId = "PAY-PROG2-001",
                        SubmissionId = "PROG2-SUB-001",
                        ApplicationId = "APP-2024-0078",
                        GrantTitle = "STEM Education Excellence Initiative",
                        AwardAmount = 275000,
                        PaymentSchedule = new[]
                        {
                            new
                            {
                                PaymentNumber = 1,
                                Amount = 55000,
                                DueDate = DateTime.UtcNow.AddMonths(1),
                                Status = "Scheduled",
                                Description = "Initial funding - 20%"
                            },
                            new
                            {
                                PaymentNumber = 2,
                                Amount = 82500,
                                DueDate = DateTime.UtcNow.AddMonths(7),
                                Status = "Pending",
                                Description = "Mid-term payment - 30%"
                            },
                            new
                            {
                                PaymentNumber = 3,
                                Amount = 82500,
                                DueDate = DateTime.UtcNow.AddMonths(13),
                                Status = "Pending",
                                Description = "Progress payment - 30%"
                            },
                            new
                            {
                                PaymentNumber = 4,
                                Amount = 55000,
                                DueDate = DateTime.UtcNow.AddMonths(24),
                                Status = "Pending",
                                Description = "Final payment - 20%"
                            }
                        },
                        PaymentMethod = "Electronic Transfer",
                        BankAccount = "****-****-****-9012",
                        TaxReporting = new
                        {
                            TaxYear = DateTime.UtcNow.Year,
                            Form1099Required = true,
                            ReportingStatus = "Pending"
                        },
                        PerformanceMilestones = new[]
                        {
                            new { Milestone = "Program Launch", DueDate = DateTime.UtcNow.AddMonths(2), Status = "Pending" },
                            new { Milestone = "50% Student Enrollment", DueDate = DateTime.UtcNow.AddMonths(8), Status = "Pending" },
                            new { Milestone = "Mid-term Evaluation", DueDate = DateTime.UtcNow.AddMonths(14), Status = "Pending" },
                            new { Milestone = "Final Report", DueDate = DateTime.UtcNow.AddMonths(25), Status = "Pending" }
                        }
                    }
                },
                PaymentSummary = new
                {
                    TotalAwardAmount = 275000,
                    TotalPaid = 0,
                    TotalPending = 275000,
                    NextPaymentDue = DateTime.UtcNow.AddMonths(1),
                    NextPaymentAmount = 55000,
                    PaymentType = "Milestone-Based",
                    RequiresPerformanceReporting = true
                }
            }
        };
    }

    private object GenerateDefaultData(object baseData)
    {
        return new
        {
            baseData,
            Data = new
            {
                Message = "Demo data available for:",
                AvailableProviders = new[] { "Program1", "Program2" },
                AvailableKeys = new[] { "Submissions", "OrgInfo", "Payments" },
                Instructions = "Use Provider and Key parameters to get specific mock data",
                Examples = new[]
                {
                    "Provider=Program1, Key=Submissions - Get grant application submissions for Program1",
                    "Provider=Program1, Key=OrgInfo - Get organization information for Program1",
                    "Provider=Program1, Key=Payments - Get payment information for Program1",
                    "Provider=Program2, Key=Submissions - Get grant application submissions for Program2",
                    "Provider=Program2, Key=OrgInfo - Get organization information for Program2",
                    "Provider=Program2, Key=Payments - Get payment information for Program2"
                }
            }
        };
    }
}
