namespace Grants.ApplicantPortal.API.Web.Contacts;

public class DeleteContactResponse
{
  public Guid ContactId { get; set; }
  public string Message { get; set; } = "Contact deleted successfully";
  public Guid? PrimaryContactId { get; set; }
}
