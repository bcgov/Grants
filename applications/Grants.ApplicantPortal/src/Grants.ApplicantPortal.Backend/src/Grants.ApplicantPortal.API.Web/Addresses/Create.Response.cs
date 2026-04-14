namespace Grants.ApplicantPortal.API.Web.Addresses;

public class CreateAddressResponse
{
  public Guid AddressId { get; set; }
  public Guid? PrimaryAddressId { get; set; }
}
