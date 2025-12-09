namespace Grants.ApplicantPortal.API.Web.Organizations;

public class UpdateOrganizationResponse
{
  public Guid OrganizationId { get; set; }
  public string Message { get; set; } = "Organization updated successfully";
}
