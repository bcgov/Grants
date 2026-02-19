namespace Grants.ApplicantPortal.API.UseCases.Contacts.Get;

public class GetContactHandler()
  : IQueryHandler<GetContactQuery, Result<ContactDto>>
{
  public async Task<Result<ContactDto>> Handle(GetContactQuery request, CancellationToken cancellationToken)
  {
    await Task.CompletedTask;
    return new ContactDto(Guid.NewGuid(), "Mock Name", "ApplicantProfile", false, true, "Mr", "email@email.com", "123-555-1234", null, null, null, null, null);
  }
}
