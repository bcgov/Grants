using Grants.ApplicantPortal.API.UseCases.Addresses;

namespace Grants.ApplicantPortal.API.UseCases.Addresses.Delete;

/// <summary>
/// Delete an existing Address.
/// </summary>
public record DeleteAddressCommand(
  Guid AddressId,
  Guid ApplicantId,
  Guid ProfileId,
  string PluginId,
  string Provider,
  string? Subject = null) : ICommand<Result<AddressMutationResult>>;
