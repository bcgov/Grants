namespace Grants.ApplicantPortal.API.Web.Contacts;

public class CreateContactResponse
{
  public Guid ContactId { get; set; }
  public string Name { get; set; } = string.Empty;
}
