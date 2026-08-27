namespace Grants.ApplicantPortal.API.Web.Addresses;

public class SetAsPrimaryAddressResponse
{
  public Guid AddressId { get; set; }
  public string Message { get; set; } = "Address set as primary successfully";
  /// <summary>
  /// The resolved primary address ID for each address type (e.g. Physical, Mailing).
  /// An applicant can hold one primary address per address type.
  /// </summary>
  public IReadOnlyDictionary<string, Guid> PrimaryAddressIdsByType { get; set; } =
    new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
}
