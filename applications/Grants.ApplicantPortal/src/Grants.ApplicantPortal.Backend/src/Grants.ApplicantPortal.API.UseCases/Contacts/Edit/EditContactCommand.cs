namespace Grants.ApplicantPortal.API.UseCases.Contacts.Edit;

/// <summary>
/// Edit an existing Contact.
/// </summary>
public record EditContactCommand(
  Guid ContactId,
  string Name,
  string ContactType,
  bool IsPrimary,
  string? Title,
  string? Email,
  string? HomePhoneNumber,
  string? MobilePhoneNumber,
  string? WorkPhoneNumber,
  string? WorkPhoneExtension,
  string? Role,
  Guid ProfileId,
  string PluginId,
  string Provider) : ICommand<Result>;
