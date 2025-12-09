namespace Grants.ApplicantPortal.API.Web.Contacts;

public record ContactRecord(Guid Id,
  string Name,
  string Type,
  bool IsPrimary,
  string? Title,
  string? Email,
  string? Phone);

