using System.Collections;

namespace Grants.ApplicantPortal.API.Plugins.Demo.Data;

/// <summary>
/// Static fixture data for the DEMO plugin's submission form retrieval.
/// Provides a form.io schema/data pair that varies per submission — looked up
/// from the same 7 static demo submissions defined in <see cref="SubmissionsData"/>
/// (<see cref="SubmissionsData.GenerateProgram1Submissions"/> and
/// <see cref="SubmissionsData.GenerateProgram2Submissions"/>) — so the PDF preview
/// for a given submission id renders plausible, submission-specific content instead
/// of identical fixture data for every submission.
/// </summary>
public static class SubmissionFormData
{
  /// <summary>
  /// Maps a demo submission's <c>type</c> (as defined in <see cref="SubmissionsData"/>)
  /// to a form.io "programType" select option value/label, and a plausible organization
  /// name for that program. Types not present here (i.e. any submission id that isn't
  /// one of the 7 known static fixtures) fall back to a generic value.
  /// </summary>
  private static readonly IReadOnlyDictionary<string, (string Value, string OrganizationName)> _programTypesByType =
      new Dictionary<string, (string Value, string OrganizationName)>
      {
        ["Community Health Initiative"] = ("communityHealth", "Coastal Community Health Society"),
        ["Youth Mental Health Support Program"] = ("youthMentalHealth", "Fraser Valley Youth Support Association"),
        ["Wellness Fitness Program"] = ("wellnessFitness", "Interior Wellness & Fitness Collective"),
        ["Digital Community Program"] = ("digitalCommunity", "Northern Digital Access Cooperative"),
        ["STEM Education Excellence Initiative"] = ("stemEducation", "Pacific STEM Education Alliance"),
        ["Digital Literacy for Seniors"] = ("digitalLiteracySeniors", "Vancouver Island Seniors Digital Literacy Network"),
        ["Rural Broadband Access Project"] = ("ruralBroadband", "Cariboo Rural Broadband Society")
      };

  /// <summary>
  /// The 7 known static demo submissions (id, type, referenceNo), sourced directly
  /// from <see cref="SubmissionsData"/> so this fixture never drifts from the grid data.
  /// </summary>
  private static readonly IReadOnlyList<(string Id, string Type, string ReferenceNo)> _knownSubmissions =
      BuildKnownSubmissions();

  private static readonly object _genericFallbackBaseData = new { };

  private static IReadOnlyList<(string Id, string Type, string ReferenceNo)> BuildKnownSubmissions()
  {
    var submissions = new List<(string Id, string Type, string ReferenceNo)>();

    AppendSubmissions(SubmissionsData.GenerateProgram1Submissions(_genericFallbackBaseData), submissions);
    AppendSubmissions(SubmissionsData.GenerateProgram2Submissions(_genericFallbackBaseData), submissions);

    return submissions;
  }

  private static void AppendSubmissions(object generated, List<(string Id, string Type, string ReferenceNo)> destination)
  {
    var submissionsProperty = generated.GetType().GetProperty("submissions");
    if (submissionsProperty?.GetValue(generated) is not IEnumerable submissions) return;

    foreach (var submission in submissions)
    {
      var submissionType = submission.GetType();
      var id = submissionType.GetProperty("id")?.GetValue(submission) as string;
      var type = submissionType.GetProperty("type")?.GetValue(submission) as string;
      var referenceNo = submissionType.GetProperty("referenceNo")?.GetValue(submission) as string;

      if (!string.IsNullOrEmpty(id))
      {
        destination.Add((id, type ?? string.Empty, referenceNo ?? string.Empty));
      }
    }
  }

  /// <summary>
  /// Returns a form.io schema and matching submission data for the given submission id.
  /// When <paramref name="submissionId"/> matches one of the 7 known static demo
  /// submissions, the returned data reflects that submission's program type and
  /// reference number. Any other id (the known submission set is not assumed to be
  /// exhaustive or immutable) falls back to generic, but still submission-id-aware, data.
  /// </summary>
  public static (object Schema, object Data) GetForm(string submissionId)
  {
    var (organizationName, programType, projectSummary) = ResolveSubmissionDetails(submissionId);

    var schema = new
    {
      display = "form",
      components = new object[]
      {
        new
        {
          type = "panel",
          key = "applicantDetailsPanel",
          title = "Applicant Details",
          input = false,
          components = new object[]
          {
            new
            {
              type = "textfield",
              key = "organizationName",
              label = "Organization Name",
              input = true
            },
            new
            {
              type = "select",
              key = "programType",
              label = "Program Type",
              input = true,
              data = new
              {
                values = _programTypesByType
                    .Select(kvp => new { label = kvp.Key, value = kvp.Value.Value })
                    .ToArray()
              }
            }
          }
        },
        new
        {
          type = "textarea",
          key = "projectSummary",
          label = "Project Summary",
          input = true
        }
      }
    };

    var data = new
    {
      data = new
      {
        submissionId,
        organizationName,
        programType,
        projectSummary
      }
    };

    return (schema, data);
  }

  private static (string OrganizationName, string ProgramType, string ProjectSummary) ResolveSubmissionDetails(string submissionId)
  {
    var known = _knownSubmissions.FirstOrDefault(
        s => string.Equals(s.Id, submissionId, StringComparison.OrdinalIgnoreCase));

    if (known.Id is not null && _programTypesByType.TryGetValue(known.Type, out var option))
    {
      var projectSummary =
          $"{known.Type} submission (Reference No. {known.ReferenceNo}) — a representative project summary used to demonstrate the submission PDF pipeline.";

      return (option.OrganizationName, option.Value, projectSummary);
    }

    // Generic fallback for any submission id that isn't one of the 7 known static fixtures.
    return (
        "Demo Community Society",
        "communityHealth",
        $"A representative project summary used to demonstrate the submission PDF pipeline for submission {submissionId}.");
  }
}
