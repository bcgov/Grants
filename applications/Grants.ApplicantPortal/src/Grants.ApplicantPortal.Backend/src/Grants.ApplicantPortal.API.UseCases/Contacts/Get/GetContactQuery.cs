namespace Grants.ApplicantPortal.API.UseCases.Contacts.Get;

public record GetContactQuery(Guid ContactId) : IQuery<Result<ContactDto>>;
