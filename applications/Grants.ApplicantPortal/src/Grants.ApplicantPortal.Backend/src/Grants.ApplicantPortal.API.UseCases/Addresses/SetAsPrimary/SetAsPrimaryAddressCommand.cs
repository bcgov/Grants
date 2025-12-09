namespace Grants.ApplicantPortal.API.UseCases.Addresses.SetAsPrimary;

/// <summary>
/// Set an address as the primary address.
/// </summary>
public record SetAsPrimaryAddressCommand(
  Guid AddressId,
  Guid ProfileId,
  string PluginId,
  string Provider) : ICommand<Result>;
