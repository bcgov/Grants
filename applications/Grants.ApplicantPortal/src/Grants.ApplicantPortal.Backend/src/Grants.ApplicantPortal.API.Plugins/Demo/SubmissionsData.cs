namespace Grants.ApplicantPortal.API.Plugins.Demo;

/// <summary>
/// Static data provider for demo submission information
/// </summary>
public static class SubmissionsData
{
  public static object GenerateProgram1Submissions(object baseData)
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
                        Id = "S1234E56-789A-BC01-23DE-F4567890AB12", // Added GUID ID
                        SubmissionId = "PROG1-SUB-001",
                        ApplicationId = "APP-2024-0001",
                        ProjectName = "Community Health Initiative",
                        ProgramName = "Program1 - Health & Wellness",
                        RequestedAmount = 150000,
                        PaidAmount = 100000,
                        Status = "In progress",
                        SubmissionDate = DateTime.UtcNow.AddDays(-15),
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
                        Id = "S2345E67-890A-BC12-34DE-F5678901AB23", // Added GUID ID
                        SubmissionId = "PROG1-SUB-002",
                        ApplicationId = "APP-2024-0002",
                        ProjectName = "Youth Mental Health Support Program",
                        ProgramName = "Program1 - Health & Wellness",
                        RequestedAmount = 85000,
                        PaidAmount = 82000,
                        Status = "Approved",
                        SubmissionDate = DateTime.UtcNow.AddDays(-45),
                        LastModified = DateTime.UtcNow.AddDays(-10),
                        ProjectPeriod = new
                        {
                            StartDate = DateTime.UtcNow.AddMonths(1),
                            EndDate = DateTime.UtcNow.AddMonths(13)
                        },
                        Categories = new[] { "Mental Health", "Youth Services", "Community Support" }
                    },
                     new
                    {
                        Id = "S3456E78-901A-BC23-45DE-F6789012AB34", // Added GUID ID
                        SubmissionId = "PROG1-SUB-003",
                        ApplicationId = "APP-2024-0003",
                        ProjectName = "Wellness Fitness Program",
                        ProgramName = "Program1 - Fitness",
                        RequestedAmount = 120000,
                        PaidAmount = 75000,
                        Status = "Declined",
                        SubmissionDate = DateTime.UtcNow.AddDays(-30),
                        LastModified = DateTime.UtcNow.AddDays(-5),
                        ProjectPeriod = new
                        {
                            StartDate = DateTime.UtcNow.AddMonths(1),
                            EndDate = DateTime.UtcNow.AddMonths(17)
                        },
                        Categories = new[] { "Healthcare", "Community Outreach", "Prevention" }
                    },
                     new
                    {
                        Id = "S4567E89-012A-BC34-56DE-F7890123AB45", // Added GUID ID
                        SubmissionId = "PROG1-SUB-004",
                        ApplicationId = "APP-2024-0004",
                        ProjectName = "Digital Community Program",
                        ProgramName = "Program1 - Digital Health",
                        RequestedAmount = 250000,
                        PaidAmount = 220000,
                        Status = "In progress",
                        SubmissionDate = DateTime.UtcNow.AddDays(-18),
                        LastModified = DateTime.UtcNow.AddDays(-1), // More recent than submission 003
                        ProjectPeriod = new
                        {
                            StartDate = DateTime.UtcNow.AddMonths(4),
                            EndDate = DateTime.UtcNow.AddMonths(14)
                        },
                        Categories = new[] { "Healthcare", "Community Outreach", "Prevention" }
                    }
                }
                .OrderByDescending(s => s.LastModified) // Sort by most recently modified first
                .ToArray(),
        Summary = new
        {
          TotalSubmissions = 4,
          TotalRequestedAmount = 605000,
          TotalPaidAmount = 477000,
          ApprovedCount = 1,
          InProgressCount = 2,
          DeclinedCount = 1
        }
      }
    };
  }

  public static object GenerateProgram2Submissions(object baseData)
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
                        Id = "S5678E90-123A-BC45-67DE-F8901234AB56", // Added GUID ID
                        SubmissionId = "PROG2-SUB-001",
                        ApplicationId = "APP-2024-0078",
                        ProjectName = "STEM Education Excellence Initiative",
                        ProgramName = "Program2 - Education & Technology",
                        RequestedAmount = 275000,
                        PaidAmount = 10000,
                        Status = "Approved",
                        SubmissionDate = DateTime.UtcNow.AddDays(-30),
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
                        Id = "S6789E01-234A-BC56-78DE-F9012345AB67", // Added GUID ID
                        SubmissionId = "PROG2-SUB-002",
                        ApplicationId = "APP-2024-0089",
                        ProjectName = "Digital Literacy for Seniors",
                        ProgramName = "Program2 - Education & Technology",
                        RequestedAmount = 120000,
                        PaidAmount = 2000,
                        Status = "Under Review",
                        SubmissionDate = DateTime.UtcNow.AddDays(-20),
                        LastModified = DateTime.UtcNow.AddDays(-1), // Most recent
                        ProjectPeriod = new
                        {
                            StartDate = DateTime.UtcNow.AddMonths(3),
                            EndDate = DateTime.UtcNow.AddMonths(15)
                        },
                        Categories = new[] { "Digital Literacy", "Senior Services", "Community Education" }
                    },
                    new
                    {
                        Id = "S7890E12-345A-BC67-89DE-F0123456AB78", // Added GUID ID
                        SubmissionId = "PROG2-SUB-003",
                        ApplicationId = "APP-2024-0095",
                        ProjectName = "Rural Broadband Access Project",
                        ProgramName = "Program2 - Education & Technology",
                        RequestedAmount = 450000,
                        PaidAmount = 40000,
                        Status = "In Review",
                        SubmissionDate = DateTime.UtcNow.AddDays(-10),
                        LastModified = DateTime.UtcNow.AddDays(-3),
                        ProjectPeriod = new
                        {
                            StartDate = DateTime.UtcNow.AddMonths(4),
                            EndDate = DateTime.UtcNow.AddMonths(28)
                        },
                        Categories = new[] { "Infrastructure", "Rural Development", "Technology Access" }
                    }
                }
                .OrderByDescending(s => s.LastModified) // Sort by most recently modified first
                .ToArray(),
        Summary = new
        {
          TotalSubmissions = 3,
          TotalRequestedAmount = 845000,
          TotalPaidAmount = 52000,
          ApprovedCount = 1,
          UnderReviewCount = 1,
          InReviewCount = 1
        }
      }
    };
  }
}
