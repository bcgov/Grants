namespace Grants.ApplicantPortal.API.Core.DTOs;

/// <summary>
/// Typed response for payments data matching the Unity PAYMENTINFO contract
/// </summary>
public record PaymentsResponse(
    IReadOnlyList<PaymentResponse> Payments
);

/// <summary>
/// Individual payment information matching the Unity payment structure
/// </summary>
public record PaymentResponse(
    string Id,
    string PaymentNumber,
    string ReferenceNo,
    decimal Amount,
    string? PaymentDate,
    string PaymentStatus
);
