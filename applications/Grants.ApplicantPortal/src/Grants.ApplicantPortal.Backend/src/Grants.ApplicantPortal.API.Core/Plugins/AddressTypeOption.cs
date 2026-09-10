namespace Grants.ApplicantPortal.API.Core.Plugins;

/// <summary>
/// Represents a selectable address type option provided by a plugin.
/// </summary>
/// <remarks>
/// The <see cref="Key"/> is normalized through <see cref="AddressTypeKey"/> on construction.
/// It travels to the client, comes back on create and edit, and is then used to group addresses
/// when enforcing one primary address per type — so it has to be the same key the grouping
/// logic produces, otherwise an option could round-trip into a group of its own.
/// </remarks>
public record AddressTypeOption(
    string Key,
    string Label
)
{
  public string Key { get; init; } = AddressTypeKey.Normalize(Key);
}
