namespace Grants.ApplicantPortal.API.Core.DTOs;

/// <summary>
/// Request DTO for editing an existing address
/// </summary>
public record EditAddressRequest(
  Guid AddressId,
  string Type,
  string Address,
  string City,
  string Province,
  string PostalCode,
  bool IsPrimary,
  string? Country = null);
