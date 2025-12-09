namespace Grants.ApplicantPortal.API.UseCases.Contacts;
public record ContactDto(Guid Id,
  string Name,
  string Type,
  bool IsPrimary,
  string? Title,
  string? Email,
  string? PhoneNumber);
