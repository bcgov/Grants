using Grants.ApplicantPortal.API.Core.Plugins;

namespace Grants.ApplicantPortal.API.UseCases.Contacts.Retrieve;

/// <summary>
/// Query to retrieve contact data from cache for a specific plugin
/// </summary>
public record RetrieveContactsQuery(
  Guid ProfileId,
  string PluginId,
  string Provider,
  Dictionary<string, object>? AdditionalData = null
 ) : IQuery<Result<ProfileData>>;
