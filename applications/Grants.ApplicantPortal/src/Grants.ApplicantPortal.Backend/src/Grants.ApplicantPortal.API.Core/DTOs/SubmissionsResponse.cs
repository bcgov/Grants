namespace Grants.ApplicantPortal.API.Core.DTOs;

/// <summary>
/// Typed response for submissions data
/// </summary>
public record SubmissionsResponse(
    IReadOnlyList<SubmissionResponse> Submissions,
    SubmissionsSummary Summary
);

/// <summary>
/// Individual submission information
/// </summary>
public record SubmissionResponse(
    string Id,
    string SubmissionId,
    string ApplicationId,
    string ProjectName,
    string ProgramName,
    decimal RequestedAmount,
    decimal PaidAmount,
    string Status,
    string StatusCode,
    DateTime SubmissionDate,
    DateTime LastModified,
    ProjectPeriod ProjectPeriod
);

/// <summary>
/// Project period information
/// </summary>
public record ProjectPeriod(
    DateTime StartDate,
    DateTime EndDate
);

/// <summary>
/// Summary information for submissions
/// </summary>
public record SubmissionsSummary(
    int TotalSubmissions,
    decimal TotalRequestedAmount,
    decimal TotalPaidAmount,
    int ApprovedCount,
    int UnderReviewCount,
    int InReviewCount
);
