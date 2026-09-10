namespace Grants.ApplicantPortal.API.UseCases.Addresses;

/// <summary>
/// Result returned by all address mutation handlers (Create, Edit, SetAsPrimary, Delete).
/// Contains the affected address ID and the resolved primary address ID of every address
/// type present in the cache, so the Web layer can simply map this to its response without
/// any cache logic. An applicant can hold one primary address per address type.
/// The dictionary is keyed by address type and compared case-insensitively.
/// </summary>
public record AddressMutationResult(Guid AddressId, IReadOnlyDictionary<string, Guid> PrimaryAddressIdsByType);
