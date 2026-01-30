namespace Grants.ApplicantPortal.API.Core.DTOs;

/// <summary>
/// Typed response for payments data
/// </summary>
public record PaymentsResponse(
    IReadOnlyList<PaymentResponse> Payments,
    PaymentsSummary PaymentSummary
);

/// <summary>
/// Individual payment information
/// </summary>
public record PaymentResponse(
    string PaymentId,
    string SubmissionId,
    string ApplicationId,
    string GrantTitle,
    decimal AwardAmount,
    IReadOnlyList<PaymentScheduleItem> PaymentSchedule,
    string PaymentMethod,
    string BankAccount,
    TaxReporting TaxReporting
);

/// <summary>
/// Payment schedule item
/// </summary>
public record PaymentScheduleItem(
    int PaymentNumber,
    decimal Amount,
    DateTime DueDate,
    string Status,
    string Description
);

/// <summary>
/// Tax reporting information
/// </summary>
public record TaxReporting(
    int TaxYear,
    bool Form1099Required,
    string ReportingStatus
);

/// <summary>
/// Payment summary information
/// </summary>
public record PaymentsSummary(
    decimal TotalAwardAmount,
    decimal TotalPaid,
    decimal TotalPending,
    DateTime NextPaymentDue,
    decimal NextPaymentAmount
);
