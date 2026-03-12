namespace Grants.ApplicantPortal.API.Web.Contacts;

public class UpdateContactResponse
{
  public Guid ContactId { get; set; }
  public string Message { get; set; } = "Contact updated successfully";
  public Guid? PrimaryContactId { get; set; }
}
