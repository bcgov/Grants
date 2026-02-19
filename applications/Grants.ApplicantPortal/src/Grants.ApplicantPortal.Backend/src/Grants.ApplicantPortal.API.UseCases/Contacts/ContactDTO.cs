namespace Grants.ApplicantPortal.API.UseCases.Contacts;
public record ContactDto(Guid ContactId,
string Name,
string ContactType,
bool IsPrimary,
bool IsEditable,
string? Title,
string? Email,
string? HomePhoneNumber,
string? MobilePhoneNumber,
string? WorkPhoneNumber,
string? WorkPhoneExtension,
string? Role,
Guid? ApplicationId);
