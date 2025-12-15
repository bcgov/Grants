namespace Grants.ApplicantPortal.API.UseCases.Contacts.Create;

/// <summary>
/// Create a new Contact.
/// </summary>
/// <param name="Name"></param>
public record CreateContactCommand(string Name,
  string Type,
  bool IsPrimary,
  string? Title,
  string? Email,
  string? PhoneNumber,
  Guid ProfileId,
  string PluginId,
  string Provider) : ICommand<Result<Guid>>;
