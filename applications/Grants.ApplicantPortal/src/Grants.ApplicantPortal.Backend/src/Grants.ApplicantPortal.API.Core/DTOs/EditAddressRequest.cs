namespace Grants.ApplicantPortal.API.Core.DTOs;

/// <summary>
/// Request DTO for editing an existing address.
/// Field names aligned with real Unity API address structure.
/// </summary>
public record EditAddressRequest(
  Guid AddressId,
  string AddressType,
  string Street,
  string City,
  string Province,
  string PostalCode,
  bool IsPrimary,
  string? Street2 = null,
  string? Unit = null,
  string? Country = null);
