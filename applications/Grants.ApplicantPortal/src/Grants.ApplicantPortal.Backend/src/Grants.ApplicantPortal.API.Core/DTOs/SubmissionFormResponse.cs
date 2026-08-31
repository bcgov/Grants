namespace Grants.ApplicantPortal.API.Core.DTOs;

/// <summary>
/// Payload returned for a single submission's rendered form. Contains the form.io
/// form definition (<c>Schema</c>) and the submitted data (<c>Data</c>) needed by the
/// frontend to render the form client-side and rasterize it to a PDF. Both properties
/// are untyped JSON blobs — matching the existing convention used by <c>ProfileData.Data</c>
/// for plugin-sourced payloads.
/// </summary>
public record SubmissionFormResponse(object Schema, object Data);
