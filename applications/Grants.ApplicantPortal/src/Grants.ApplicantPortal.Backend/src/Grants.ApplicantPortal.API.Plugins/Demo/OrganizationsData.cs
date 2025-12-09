namespace Grants.ApplicantPortal.API.Plugins.Demo;

/// <summary>
/// Static data provider for demo organization information
/// </summary>
public static class OrganizationsData
{
  public static object GenerateProgram1OrgInfo(object baseData)
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
          OrgStatus = "Active",
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
            State = "BC",
            ZipCode = "V8W 2Y7",
            Country = "Canada"
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

  public static object GenerateProgram2OrgInfo(object baseData)
  {
    return new
    {
      baseData,
      Data = new
      {
        OrganizationInfo = new
        {
          OrgName = "Hub Tech Solutions",
          OrgNumber = "S1113734",
          OrgStatus = "Active",
          OrganizationType = "Educational Nonprofit",
          NonRegOrgName = "Digital Innovation Org",
          OrgSize = "30",
          FiscalMonth = "Jul",
          FiscalDay = 23,
          OrganizationId = "ORG-DEMO-002",
          LegalName = "Demo Educational Technology Consortium",
          DoingBusinessAs = "DETC",
          EIN = "98-7654321",
          Founded = 2015,
          Address = new
          {
            Street = "456 Innovation Drive",
            City = "Tech Valley",
            State = "AB",
            ZipCode = "T2P 4K6",
            Country = "Canada"
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

  public static object GenerateProgram1Payments(object baseData)
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

  public static object GenerateDefaultData(object baseData)
  {
    var (providers, keys) = DemoPluginFeatures.GetProvidersAndKeys();
    
    return new
    {
      baseData,
      Data = new
      {
        Message = "Demo data available for:",
        AvailableProviders = providers,
        AvailableKeys = keys,
        Instructions = "Use Provider and Key parameters to get specific mock data",
        SupportedCombinations = DemoPluginFeatures.GetAvailableCombinationsDescription(),
        Examples = new[]
            {
                    "Provider=Program1, Key=Submissions - Get grant application submissions for Program1",
                    "Provider=Program1, Key=OrgInfo - Get organization information for Program1",
                    "Provider=Program1, Key=Payments - Get payment information for Program1",
                    "Provider=Program1, Key=Contacts - Get contact information for Program1",
                    "Provider=Program1, Key=Addresses - Get address information for Program1",
                    "Provider=Program2, Key=Submissions - Get grant application submissions for Program2",
                    "Provider=Program2, Key=OrgInfo - Get organization information for Program2",
                    "Provider=Program2, Key=Contacts - Get contact information for Program2",
                    "Provider=Program2, Key=Addresses - Get address information for Program2"
                }
      }
    };
  }
}
