namespace Grants.ApplicantPortal.API.UseCases.Contacts.Delete;

/// <summary>
/// Delete an existing Contact.
/// </summary>
public record DeleteContactCommand(
  Guid ContactId,
  Guid ProfileId,
  string PluginId,
  string Provider) : ICommand<Result>;
