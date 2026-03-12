namespace Grants.ApplicantPortal.API.Plugins.Demo.Data;

/// <summary>
/// Static data provider for demo submission information.
/// Field names match the Unity API contract (id, linkId, receivedTime,
/// submissionTime, referenceNo, projectName, status) plus linkSource.
/// </summary>
public static class SubmissionsData
{
  public static object GenerateProgram1Submissions(object baseData)
  {
    return new
    {
      submissions = new[]
      {
        new
        {
          id = "a1234e56-789a-bc01-23de-f4567890ab12",
          linkId = "b1234e56-789a-bc01-23de-f4567890ab12",
          receivedTime = "2025-06-15T22:51:06.241061Z",
          submissionTime = "2025-06-15T22:42:24.115Z",
          referenceNo = "B1234E56",
          projectName = "Community Health Initiative",
          status = "Submitted"
        },
        new
        {
          id = "a2345e67-890a-bc12-34de-f5678901ab23",
          linkId = "b2345e67-890a-bc12-34de-f5678901ab23",
          receivedTime = "2025-05-01T18:30:00Z",
          submissionTime = "2025-05-01T18:20:15.5Z",
          referenceNo = "B2345E67",
          projectName = "Youth Mental Health Support Program",
          status = "Under Review"
        },
        new
        {
          id = "a3456e78-901a-bc23-45de-f6789012ab34",
          linkId = "b3456e78-901a-bc23-45de-f6789012ab34",
          receivedTime = "2025-05-16T14:05:22.832421Z",
          submissionTime = "2025-05-16T13:55:10.974Z",
          referenceNo = "B3456E78",
          projectName = "Wellness Fitness Program",
          status = "Submitted"
        },
        new
        {
          id = "a4567e89-012a-bc34-56de-f7890123ab45",
          linkId = "b4567e89-012a-bc34-56de-f7890123ab45",
          receivedTime = "2025-06-28T20:12:47.914247Z",
          submissionTime = "2025-06-28T19:58:33.29Z",
          referenceNo = "B4567E89",
          projectName = "Digital Community Program",
          status = "Under Review"
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
        new
        {
          id = "c5678e90-123a-bc45-67de-f8901234ab56",
          linkId = "d5678e90-123a-bc45-67de-f8901234ab56",
          receivedTime = "2025-05-16T21:53:07.791002Z",
          submissionTime = "2025-05-16T20:57:37.29Z",
          referenceNo = "D5678E90",
          projectName = "STEM Education Excellence Initiative",
          status = "Under Review"
        },
        new
        {
          id = "c6789e01-234a-bc56-78de-f9012345ab67",
          linkId = "d6789e01-234a-bc56-78de-f9012345ab67",
          receivedTime = "2025-06-25T22:17:58.832421Z",
          submissionTime = "2025-06-25T21:37:52.974Z",
          referenceNo = "D6789E01",
          projectName = "Digital Literacy for Seniors",
          status = "Submitted"
        },
        new
        {
          id = "c7890e12-345a-bc67-89de-f0123456ab78",
          linkId = "d7890e12-345a-bc67-89de-f0123456ab78",
          receivedTime = "2025-06-10T17:34:47.914247Z",
          submissionTime = "2025-06-10T16:50:22.115Z",
          referenceNo = "D7890E12",
          projectName = "Rural Broadband Access Project",
          status = "Submitted"
        }
      },
      linkSource = "https://demo-forms.example.com/app/user/view?s="
    };
  }
}
