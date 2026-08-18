namespace Grants.ApplicantPortal.API.Core.DTOs;

/// <summary>
/// Typed response for submissions data returned by the Unity API.
/// The <c>data</c> envelope from the Unity profile endpoint contains
/// a <c>submissions</c> array and an optional <c>linkSource</c> URL prefix.
/// </summary>
public record SubmissionsResponse(
    string DataType,
    IReadOnlyList<SubmissionResponse> Submissions,
    string? LinkSource
);

/// <summary>
/// Individual submission returned by the Unity API.
/// </summary>
public record SubmissionResponse(
    string Id,
    string LinkId,
    DateTime ReceivedTime,
    DateTime SubmissionTime,
    string ReferenceNo,
    string Type,
    string Status,
    SubmissionLinkResponse? RenewalLink,
    IReadOnlyList<SubmissionLinkResponse> RelatedLinks,
    string? ApplicantMessage,
    bool EligibleForRenewal
);

/// <summary>
/// A published link (renewal or related) attached to a submission.
/// RelatedLinks are ordered by Order asc, with Order == -1 sorted last.
/// Description is only shown in the UI for related links — the renewal
/// link's description is not displayed.
/// </summary>
public record SubmissionLinkResponse(
    string Uri,
    string Title,
    string Description,
    int Order
);
