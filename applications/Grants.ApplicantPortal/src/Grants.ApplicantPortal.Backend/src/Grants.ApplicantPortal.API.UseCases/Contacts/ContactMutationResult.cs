namespace Grants.ApplicantPortal.API.UseCases.Contacts;

/// <summary>
/// Result returned by all contact mutation handlers (Create, Edit, Delete, SetAsPrimary).
/// Contains the affected contact ID and the resolved primary contact ID from the cache,
/// so the Web layer can simply map this to its response without any cache logic.
/// </summary>
public record ContactMutationResult(Guid ContactId, Guid? PrimaryContactId);
