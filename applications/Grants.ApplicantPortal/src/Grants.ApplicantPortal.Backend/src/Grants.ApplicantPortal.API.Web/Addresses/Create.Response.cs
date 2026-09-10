namespace Grants.ApplicantPortal.API.Web.Addresses;

public class CreateAddressResponse
{
  public Guid AddressId { get; set; }
  /// <summary>
  /// The resolved primary address ID for each address type (e.g. Physical, Mailing).
  /// An applicant can hold one primary address per address type.
  /// </summary>
  public IReadOnlyDictionary<string, Guid> PrimaryAddressIdsByType { get; set; } =
    new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
}
