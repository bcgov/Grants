namespace Grants.ApplicantPortal.API.Web.Submissions;

public class RetrieveSubmissionFormRequest
{
  public const string Route = "/Submissions/{PluginId}/{Provider}/{SubmissionId:Guid}/Form";
  public static string BuildRoute(string pluginId, string provider, Guid submissionId)
    => Route.Replace("{PluginId}", pluginId)
            .Replace("{Provider}", provider)
            .Replace("{SubmissionId:Guid}", submissionId.ToString());

  /// <summary>
  /// Plugin identifier for plugin-specific submission form retrieval
  /// </summary>
  public string PluginId { get; set; } = string.Empty;

  /// <summary>
  /// Provider name provided by the plugin for specific submission data retrieval
  /// </summary>
  public string Provider { get; set; } = string.Empty;

  /// <summary>
  /// Identifier of the submission to retrieve the form.io schema and data for
  /// </summary>
  public Guid SubmissionId { get; set; }
}
