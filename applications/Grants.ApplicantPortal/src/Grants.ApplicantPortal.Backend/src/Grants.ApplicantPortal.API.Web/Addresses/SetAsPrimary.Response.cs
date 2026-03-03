namespace Grants.ApplicantPortal.API.Web.Addresses;

public class SetAsPrimaryAddressResponse
{
  public Guid AddressId { get; set; }
  public string Message { get; set; } = "Address set as primary successfully";
}
