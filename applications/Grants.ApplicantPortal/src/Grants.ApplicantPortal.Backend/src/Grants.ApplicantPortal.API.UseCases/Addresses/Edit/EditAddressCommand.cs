namespace Grants.ApplicantPortal.API.UseCases.Addresses.Edit;

/// <summary>
/// Edit an existing Address.
/// </summary>
public record EditAddressCommand(
  Guid AddressId,
  string Type,
  string Address,
  string City,
  string Province,
  string PostalCode,
  bool IsPrimary,
  string? Country,
  Guid ProfileId,
  string PluginId,
  string Provider) : ICommand<Result>;
