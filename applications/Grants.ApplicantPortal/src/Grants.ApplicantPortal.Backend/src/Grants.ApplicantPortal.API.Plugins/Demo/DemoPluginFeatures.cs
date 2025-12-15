using Grants.ApplicantPortal.API.Core.Plugins;

namespace Grants.ApplicantPortal.API.Plugins.Demo;

/// <summary>
/// Static class that defines supported features for the Demo profile plugin
/// </summary>
public static class DemoPluginFeatures
{
  /// <summary>
  /// All supported features for the Demo plugin
  /// </summary>
  public static readonly IReadOnlyList<PluginSupportedFeature> SupportedFeatures = new List<PluginSupportedFeature>
    {
        // Legacy keys for backwards compatibility
        new("PROGRAM1", "SUBMISSIONS", "Demo submissions data for Program1"),
        new("PROGRAM1", "ORGINFO", "Demo organization information for Program1"),
        new("PROGRAM1", "PAYMENTS", "Demo payment information for Program1"),
        new("PROGRAM2", "SUBMISSIONS", "Demo submissions data for Program2"),
        new("PROGRAM2", "ORGINFO", "Demo organization information for Program2"),
        
        // New specific endpoint keys
        new("PROGRAM1", "CONTACTS", "Demo contacts data for Program1"),
        new("PROGRAM1", "ADDRESSES", "Demo address data for Program1"),
        new("PROGRAM2", "CONTACTS", "Demo contacts data for Program2"),
        new("PROGRAM2", "ADDRESSES", "Demo address data for Program2")
    };

  /// <summary>
  /// Gets all unique providers supported by this plugin
  /// </summary>
  /// <returns>List of supported provider names</returns>
  public static IReadOnlyList<string> GetSupportedProviders()
  {
    return SupportedFeatures
        .Select(f => f.Provider)
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToList();
  }

  /// <summary>
  /// Gets all supported keys for a specific provider
  /// </summary>
  /// <param name="provider">The provider name to get keys for</param>
  /// <returns>List of supported keys for the provider</returns>
  public static IReadOnlyList<string> GetSupportedKeys(string provider)
  {
    if (string.IsNullOrWhiteSpace(provider))
      return new List<string>();

    return SupportedFeatures
        .Where(f => f.Provider.Equals(provider, StringComparison.OrdinalIgnoreCase))
        .Select(f => f.Key)
        .ToList();
  }

  /// <summary>
  /// Checks if a provider/key combination is supported
  /// </summary>
  /// <param name="provider">The provider name</param>
  /// <param name="key">The key name</param>
  /// <returns>True if the combination is supported, false otherwise</returns>
  public static bool IsProviderKeySupported(string provider, string key)
  {
    return SupportedFeatures.Any(f =>
        f.Provider.Equals(provider, StringComparison.OrdinalIgnoreCase) &&
        f.Key.Equals(key, StringComparison.OrdinalIgnoreCase));
  }

  /// <summary>
  /// Gets all available provider/key combinations as a formatted string for documentation
  /// </summary>
  /// <returns>Formatted string listing all supported combinations</returns>
  public static string GetAvailableCombinationsDescription()
  {
    var combinations = SupportedFeatures
        .Select(f => $"Provider={f.Provider}, Key={f.Key} - {f.Description}")
        .ToList();

    return string.Join("\n", combinations);
  }

  /// <summary>
  /// Gets distinct providers and keys for summary information
  /// </summary>
  /// <returns>A tuple containing lists of providers and keys</returns>
  public static (IReadOnlyList<string> Providers, IReadOnlyList<string> Keys) GetProvidersAndKeys()
  {
    var providers = SupportedFeatures
        .Select(f => f.Provider)
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .OrderBy(p => p)
        .ToList();

    var keys = SupportedFeatures
        .Select(f => f.Key)
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .OrderBy(k => k)
        .ToList();

    return (providers, keys);
  }
}
