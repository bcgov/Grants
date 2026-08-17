namespace Grants.ApplicantPortal.API.Plugins.Demo.Data;

/// <summary>
/// A published link (renewal or related) attached to a submission.
/// Field names match the Unity API SUBMISSIONINFO contract (uri, title,
/// description, order). RelatedLinks are ordered by Order asc, with
/// Order == -1 sorted last.
/// </summary>
public sealed record SubmissionLink(string Uri, string Title, string Description, int Order);

/// <summary>
/// Static data provider for demo submission information.
/// Field names match the Unity API contract (id, linkId, receivedTime,
/// submissionTime, referenceNo, type, status, renewalLink, relatedLinks,
/// applicantMessage, eligibleForRenewal) plus linkSource.
/// RenewalLink is nullable — only present when the applicant is eligible
/// for renewal and a Published Renewal link exists on the form.
/// RelatedLinks is always present but may be an empty array.
/// ApplicantMessage is nullable free-text feedback for the submission.
/// EligibleForRenewal reflects renewal eligibility independent of whether
/// a Published Renewal link exists.
/// </summary>
public static class SubmissionsData
{
  public static object GenerateProgram1Submissions(object baseData)
  {
    return new
    {
      submissions = new[]
      {
        // Scenario: full data — renewal link plus multiple related links
        new
        {
          id = "a1234e56-789a-bc01-23de-f4567890ab12",
          linkId = "b1234e56-789a-bc01-23de-f4567890ab12",
          receivedTime = "2025-06-15T22:51:06.241061Z",
          submissionTime = "2025-06-15T22:42:24.115Z",
          referenceNo = "B1234E56",
          type = "Community Health Initiative",
          status = "Submitted",
          renewalLink = (SubmissionLink?)new SubmissionLink(
            "https://demo-forms.example.com/app/form/renew?f=community-health",
            "Renew Community Health Initiative",
            "Start your renewal application for this grant program.",
            0),
          relatedLinks = new[]
          {
            new SubmissionLink(
              "https://demo-forms.example.com/guidelines/community-health",
              "Program Guidelines",
              "Read the eligibility and reporting guidelines for this program.",
              0),
            new SubmissionLink(
              "https://demo-forms.example.com/faq/community-health",
              "Frequently Asked Questions",
              "Answers to common questions about this submission.",
              1)
          },
          applicantMessage = (string?)null,
          eligibleForRenewal = true
        },
        // Scenario: no renewal link, one related link
        new
        {
          id = "a2345e67-890a-bc12-34de-f5678901ab23",
          linkId = "b2345e67-890a-bc12-34de-f5678901ab23",
          receivedTime = "2025-05-01T18:30:00Z",
          submissionTime = "2025-05-01T18:20:15.5Z",
          referenceNo = "B2345E67",
          type = "Youth Mental Health Support Program",
          status = "Under Review",
          renewalLink = (SubmissionLink?)null,
          relatedLinks = new[]
          {
            new SubmissionLink(
              "https://demo-forms.example.com/guidelines/youth-mental-health",
              "Program Guidelines",
              "Read the eligibility and reporting guidelines for this program.",
              0)
          },
          applicantMessage = (string?)"Your submission is currently under review. We will follow up with next steps once the review is complete.",
          eligibleForRenewal = false
        },
        // Scenario: missing links entirely — not eligible for renewal, no related links
        new
        {
          id = "a3456e78-901a-bc23-45de-f6789012ab34",
          linkId = "b3456e78-901a-bc23-45de-f6789012ab34",
          receivedTime = "2025-05-16T14:05:22.832421Z",
          submissionTime = "2025-05-16T13:55:10.974Z",
          referenceNo = "B3456E78",
          type = "Wellness Fitness Program",
          status = "Submitted",
          renewalLink = (SubmissionLink?)null,
          relatedLinks = Array.Empty<SubmissionLink>(),
          applicantMessage = (string?)null,
          eligibleForRenewal = false
        },
        // Scenario: renewal link only, no related links
        new
        {
          id = "a4567e89-012a-bc34-56de-f7890123ab45",
          linkId = "b4567e89-012a-bc34-56de-f7890123ab45",
          receivedTime = "2025-06-28T20:12:47.914247Z",
          submissionTime = "2025-06-28T19:58:33.29Z",
          referenceNo = "B4567E89",
          type = "Digital Community Program",
          status = "Under Review",
          renewalLink = (SubmissionLink?)new SubmissionLink(
            "https://demo-forms.example.com/app/form/renew?f=digital-community",
            "Renew Digital Community Program",
            "Start your renewal application for this grant program.",
            0),
          relatedLinks = Array.Empty<SubmissionLink>(),
          applicantMessage = (string?)"You are eligible to renew this grant. Please use the renewal link above to start your application.",
          eligibleForRenewal = true
        }
      },
      linkSource = "https://demo-forms.example.com/app/user/view?s="
    };
  }

  public static object GenerateProgram2Submissions(object baseData)
  {
    return new
    {
      submissions = new[]
      {
        // Scenario: renewal link plus several related links, including an
        // unordered (Order == -1) link that must sort last
        new
        {
          id = "c5678e90-123a-bc45-67de-f8901234ab56",
          linkId = "d5678e90-123a-bc45-67de-f8901234ab56",
          receivedTime = "2025-05-16T21:53:07.791002Z",
          submissionTime = "2025-05-16T20:57:37.29Z",
          referenceNo = "D5678E90",
          type = "STEM Education Excellence Initiative",
          status = "Under Review",
          renewalLink = (SubmissionLink?)new SubmissionLink(
            "https://demo-forms.example.com/app/form/renew?f=stem-education",
            "Renew STEM Education Excellence Initiative",
            "Start your renewal application for this grant program.",
            0),
          relatedLinks = new[]
          {
            new SubmissionLink(
              "https://demo-forms.example.com/guidelines/stem-education",
              "Program Guidelines",
              "Read the eligibility and reporting guidelines for this program.",
              0),
            new SubmissionLink(
              "https://demo-forms.example.com/faq/stem-education",
              "Frequently Asked Questions",
              "Answers to common questions about this submission.",
              1),
            new SubmissionLink(
              "https://demo-forms.example.com/misc/stem-education",
              "Additional Resources",
              "Unordered link — should always render last.",
              -1)
          },
          applicantMessage = (string?)"Your report has been reviewed and approved. Thank you for your continued participation in this program.",
          eligibleForRenewal = true
        },
        // Scenario: missing links entirely
        new
        {
          id = "c6789e01-234a-bc56-78de-f9012345ab67",
          linkId = "d6789e01-234a-bc56-78de-f9012345ab67",
          receivedTime = "2025-06-25T22:17:58.832421Z",
          submissionTime = "2025-06-25T21:37:52.974Z",
          referenceNo = "D6789E01",
          type = "Digital Literacy for Seniors",
          status = "Submitted",
          renewalLink = (SubmissionLink?)null,
          relatedLinks = Array.Empty<SubmissionLink>(),
          applicantMessage = (string?)null,
          eligibleForRenewal = false
        },
        // Scenario: no renewal link, two related links
        new
        {
          id = "c7890e12-345a-bc67-89de-f0123456ab78",
          linkId = "d7890e12-345a-bc67-89de-f0123456ab78",
          receivedTime = "2025-06-10T17:34:47.914247Z",
          submissionTime = "2025-06-10T16:50:22.115Z",
          referenceNo = "D7890E12",
          type = "Rural Broadband Access Project",
          status = "Submitted",
          renewalLink = (SubmissionLink?)null,
          relatedLinks = new[]
          {
            new SubmissionLink(
              "https://demo-forms.example.com/guidelines/rural-broadband",
              "Program Guidelines",
              "Read the eligibility and reporting guidelines for this program.",
              0),
            new SubmissionLink(
              "https://demo-forms.example.com/contact/rural-broadband",
              "Contact Program Staff",
              "Reach out to program staff with questions about this submission.",
              1)
          },
          applicantMessage = (string?)"Your submission is currently under review. We will follow up with next steps once the review is complete.",
          eligibleForRenewal = false
        }
      },
      linkSource = "https://demo-forms.example.com/app/user/view?s="
    };
  }
}
