namespace Grants.ApplicantPortal.API.Web.Contacts;

public class SetAsPrimaryContactResponse
{
  public Guid ContactId { get; set; }
  public string Message { get; set; } = "Contact set as primary successfully";
  public Guid? PrimaryContactId { get; set; }
}
