namespace Grants.ApplicantPortal.API.Core.DTOs;

/// <summary>
/// Request DTO for creating a new address
/// </summary>
public record CreateAddressRequest(
  string AddressType,
  string Street,
  string City,
  string Province,
  string PostalCode,
  bool IsPrimary,
  string? Street2 = null,
  string? Unit = null,
  string? Country = null,
  Guid ApplicantId = default);
