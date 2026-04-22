namespace Grants.ApplicantPortal.API.Web.Addresses;

public class DeleteAddressResponse
{
  public Guid AddressId { get; set; }
  public string Message { get; set; } = "Address deleted successfully";
  public Guid? PrimaryAddressId { get; set; }
}
