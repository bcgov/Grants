namespace Grants.ApplicantPortal.API.UseCases.Contacts.Edit;

/// <summary>
/// Edit an existing Contact.
/// </summary>
public record EditContactCommand(
  Guid ContactId,
  string Name,
  string Type,
  bool IsPrimary,
  string? Title,
  string? Email,
  string? PhoneNumber,
  Guid ProfileId,
  string PluginId,
  string Provider) : ICommand<Result>;
