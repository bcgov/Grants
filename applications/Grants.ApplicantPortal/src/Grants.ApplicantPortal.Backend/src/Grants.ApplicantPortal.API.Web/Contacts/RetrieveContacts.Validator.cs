using FluentValidation;
using Grants.ApplicantPortal.API.Plugins;

namespace Grants.ApplicantPortal.API.Web.Contacts;

/// <summary>
/// Validator for RetrieveContactsRequest
/// </summary>
public class RetrieveContactsValidator : Validator<RetrieveContactsRequest>
{
  public RetrieveContactsValidator()
  {
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

  private static bool BeValidDictionary(Dictionary<string, object>? parameters)
  {
    return true;
  }
}
