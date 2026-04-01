namespace Grants.ApplicantPortal.API.UseCases.Addresses;

/// <summary>
/// Result returned by all address mutation handlers (Edit, SetAsPrimary).
/// Contains the affected address ID and the resolved primary address ID from the cache,
/// so the Web layer can simply map this to its response without any cache logic.
/// </summary>
public record AddressMutationResult(Guid AddressId, Guid? PrimaryAddressId);
