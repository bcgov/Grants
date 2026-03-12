using Grants.ApplicantPortal.API.UseCases.Contacts;

namespace Grants.ApplicantPortal.API.UseCases.Contacts.Create;

/// <summary>
/// Create a new Contact.
/// </summary>
/// <param name="Name"></param>
public record CreateContactCommand(string Name,
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
  string Provider,
  string? Subject = null) : ICommand<Result<ContactMutationResult>>;
