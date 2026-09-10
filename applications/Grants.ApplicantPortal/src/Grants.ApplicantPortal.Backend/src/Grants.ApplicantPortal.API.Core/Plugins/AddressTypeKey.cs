namespace Grants.ApplicantPortal.API.Core.Plugins;

/// <summary>
/// Single definition of how an address type string is turned into a grouping key.
/// An applicant may hold one primary address per address type, so the plugins that enforce
/// that invariant and the resolver that reports it must agree exactly on what "same type"
/// means — otherwise a group can end up with two primaries, or none.
/// </summary>
/// <remarks>
/// An address whose type is genuinely "Unknown" groups together with untyped addresses.
/// That collision is harmless: they are treated as one type group, which is the intent.
/// </remarks>
public static class AddressTypeKey
{
  /// <summary>
  /// Key used for addresses with a missing, empty or whitespace-only address type, so a
  /// malformed entry still forms one consistent group instead of breaking resolution.
  /// </summary>
  public const string Unknown = "Unknown";

  /// <summary>
  /// Normalizes an address type into its grouping key. Surrounding whitespace is trimmed and
  /// a blank value becomes <see cref="Unknown"/>. Casing is preserved — compare keys with
  /// <see cref="AreSame"/> or an <see cref="StringComparer.OrdinalIgnoreCase"/> collection.
  /// </summary>
  public static string Normalize(string? addressType)
    => string.IsNullOrWhiteSpace(addressType) ? Unknown : addressType.Trim();

  /// <summary>
  /// Returns true when both values belong to the same address type group.
  /// Comparison is case-insensitive and no type value is ever hardcoded.
  /// </summary>
  public static bool AreSame(string? left, string? right)
    => string.Equals(Normalize(left), Normalize(right), StringComparison.OrdinalIgnoreCase);
}
