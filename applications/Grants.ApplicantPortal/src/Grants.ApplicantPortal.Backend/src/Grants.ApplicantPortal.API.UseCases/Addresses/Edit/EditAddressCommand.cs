namespace Grants.ApplicantPortal.API.UseCases.Addresses.Edit;

/// <summary>
/// Edit an existing Address.
/// Field names aligned with real Unity API address structure.
/// </summary>
public record EditAddressCommand(
  Guid AddressId,
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
