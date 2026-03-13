namespace Grants.ApplicantPortal.API.Web.Addresses;

public class UpdateAddressResponse
{
  public Guid AddressId { get; set; }
  public string Message { get; set; } = "Address updated successfully";
  public Guid? PrimaryAddressId { get; set; }
}
