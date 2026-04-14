namespace Grants.ApplicantPortal.API.UseCases.Addresses.Create;

/// <summary>
/// Create a new Address.
/// </summary>
public record CreateAddressCommand(
  string AddressType,
  string Street,
  string City,
  string Province,
  string PostalCode,
  bool IsPrimary,
  string? Street2,
  string? Unit,
  string? Country,
  Guid ProfileId,
  string PluginId,
  string Provider,
  Guid ApplicantId,
  string? Subject = null) : ICommand<Result<AddressMutationResult>>;
