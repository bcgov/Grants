using Grants.ApplicantPortal.API.Core.Plugins;

namespace Grants.ApplicantPortal.API.UseCases.Submissions.RetrieveForm;

/// <summary>
/// Query to retrieve the form.io schema and submission data for a single submission,
/// sourced from cache or hydrated on-demand via the specified plugin.
/// </summary>
public record RetrieveSubmissionFormQuery(
 Guid ProfileId,
 string PluginId,
 string Provider,
 Guid SubmissionId,
 string Subject
) : IQuery<Result<ProfileData>>;
