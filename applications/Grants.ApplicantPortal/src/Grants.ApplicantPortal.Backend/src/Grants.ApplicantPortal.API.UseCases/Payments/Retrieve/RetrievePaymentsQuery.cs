using Grants.ApplicantPortal.API.Core.Plugins;

namespace Grants.ApplicantPortal.API.UseCases.Payments.Retrieve;

/// <summary>
/// Query to retrieve payment data from cache for a specific plugin
/// </summary>
public record RetrievePaymentsQuery(
 Guid ProfileId,
 string PluginId,
 string Provider,
 string Subject,
 Dictionary<string, object>? AdditionalData = null
) : IQuery<Result<ProfileData>>;
