using FluentValidation;
using Grants.ApplicantPortal.API.Plugins;

namespace Grants.ApplicantPortal.API.Web.Profiles;

/// <summary>
/// Validator for RetrieveProfileRequest (RetrieveProfile endpoint)
/// See: https://fast-endpoints.com/docs/validation
/// </summary>
public class RetrieveProfileValidator : Validator<RetrieveProfileRequest>
{
  public RetrieveProfileValidator()
  {
    RuleFor(x => x.ProfileId)
      .NotEqual(Guid.Empty)
      .WithMessage("ProfileId must be a valid GUID");

    RuleFor(x => x.PluginId)
      .Must(BeValidPluginId)
      .When(x => !string.IsNullOrEmpty(x.PluginId))
      .WithMessage("PluginId must be a non-empty string when provided")
      .Must(BeValidRegisteredPlugin)
      .When(x => !string.IsNullOrWhiteSpace(x.PluginId))
      .WithMessage(x => $"PluginId '{x.PluginId}' is not a registered plugin. Valid plugins: {string.Join(", ", PluginRegistry.GetAllPluginIds())}");

    RuleFor(x => x.Provider)
      .Must(BeValidProvider)
      .When(x => !string.IsNullOrWhiteSpace(x.PluginId))
      .WithMessage("Provider must be a non-empty string when PluginId is provided")
      .Must((request, provider) => BeValidProviderForPlugin(request.PluginId, provider))
      .When(x => !string.IsNullOrWhiteSpace(x.PluginId) && !string.IsNullOrWhiteSpace(x.Provider))
      .WithMessage(x => $"Provider '{x.Provider}' is not supported by plugin '{x.PluginId}'. Valid providers for this plugin: {GetValidProvidersMessage(x.PluginId)}");

    RuleFor(x => x.Key)
      .Must(BeValidKey)
      .When(x => !string.IsNullOrWhiteSpace(x.PluginId) && !string.IsNullOrWhiteSpace(x.Provider))
      .WithMessage("Key must be a non-empty string when PluginId and Provider are provided")
      .Must((request, key) => BeValidKeyForProviderAndPlugin(request.PluginId, request.Provider, key))
      .When(x => !string.IsNullOrWhiteSpace(x.PluginId) && !string.IsNullOrWhiteSpace(x.Provider) && !string.IsNullOrWhiteSpace(x.Key))
      .WithMessage(x => $"Key '{x.Key}' is not supported by provider '{x.Provider}' in plugin '{x.PluginId}'. Valid keys for this provider: {GetValidKeysMessage(x.PluginId, x.Provider)}");

    RuleFor(x => x.Parameters)
      .Must(BeValidDictionary)
      .When(x => x.Parameters != null)
      .WithMessage("Parameters must be a valid dictionary when provided");
  }

  private static bool BeValidPluginId(string? pluginId)
  {
    return !string.IsNullOrWhiteSpace(pluginId);
  }

  private static bool BeValidRegisteredPlugin(string? pluginId)
  {
    return PluginRegistry.IsValidPluginId(pluginId);
  }

  private static bool BeValidProvider(string? provider)
  {
    return !string.IsNullOrWhiteSpace(provider);
  }

  private static bool BeValidProviderForPlugin(string pluginId, string? provider)
  {
    if (string.IsNullOrWhiteSpace(provider))
      return false;

    var pluginInfo = PluginRegistry.GetPluginInfo(pluginId);
    if (pluginInfo == null)
      return false;

    return pluginInfo.SupportedFeatures.Any(f => 
      f.Provider.Equals(provider, StringComparison.OrdinalIgnoreCase));
  }

  private static bool BeValidKey(string? key)
  {
    return !string.IsNullOrWhiteSpace(key);
  }

  private static bool BeValidKeyForProviderAndPlugin(string pluginId, string provider, string? key)
  {
    if (string.IsNullOrWhiteSpace(key))
      return false;

    return PluginRegistry.IsValidProviderKey(pluginId, provider, key);
  }

  private static string GetValidProvidersMessage(string pluginId)
  {
    var pluginInfo = PluginRegistry.GetPluginInfo(pluginId);
    if (pluginInfo == null)
      return "none";

    var providers = pluginInfo.SupportedFeatures
      .Select(f => f.Provider)
      .Distinct(StringComparer.OrdinalIgnoreCase)
      .OrderBy(p => p)
      .ToList();

    return providers.Any() ? string.Join(", ", providers) : "none";
  }

  private static string GetValidKeysMessage(string pluginId, string provider)
  {
    var pluginInfo = PluginRegistry.GetPluginInfo(pluginId);
    if (pluginInfo == null)
      return "none";

    var keys = pluginInfo.SupportedFeatures
      .Where(f => f.Provider.Equals(provider, StringComparison.OrdinalIgnoreCase))
      .Select(f => f.Key)
      .OrderBy(k => k)
      .ToList();

    return keys.Any() ? string.Join(", ", keys) : "none";
  }

  private static bool BeValidDictionary(Dictionary<string, object>? parameters)
  {
    // Since we're using a typed Dictionary, basic validation is sufficient
    // Additional business rules can be added here if needed
    return true;
  }
}
