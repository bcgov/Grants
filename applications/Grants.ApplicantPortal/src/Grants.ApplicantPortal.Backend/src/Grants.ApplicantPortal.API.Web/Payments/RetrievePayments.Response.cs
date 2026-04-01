namespace Grants.ApplicantPortal.API.Web.Payments;

public record RetrievePaymentsResponse(
    Guid ProfileId,
    string PluginId,
    string Provider,
    object Data,
    DateTime PopulatedAt,
    string? CacheStatus = null,
    string? CacheStore = null
);
