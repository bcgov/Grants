using Grants.ApplicantPortal.API.UseCases.Contacts;

namespace Grants.ApplicantPortal.API.UseCases.Contacts.SetAsPrimary;

/// <summary>
/// Set a contact as the primary contact.
/// </summary>
public record SetAsPrimaryContactCommand(
  Guid ContactId,
  Guid ApplicantId,
  Guid ProfileId,
  string PluginId,
  string Provider,  
  string? Subject = null) : ICommand<Result<ContactMutationResult>>;
