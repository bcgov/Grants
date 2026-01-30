using System.Text.Json;

namespace Grants.ApplicantPortal.API.Core.Plugins;

/// <summary>
/// Configuration options for plugins loaded from appsettings
/// </summary>
public class PluginConfiguration : Dictionary<string, PluginOptions>
{
    public const string SectionName = "Plugins";
    
    public PluginConfiguration() : base(StringComparer.OrdinalIgnoreCase)
    {
    }
}

/// <summary>
/// Configuration options for an individual plugin
/// Standard properties: Enabled, Features, and Providers are common to all plugins
/// Configuration: Generic JSON element that each plugin can define its own structure
/// </summary>
public class PluginOptions
{
    public bool Enabled { get; set; } = true;
    public List<string> Features { get; set; } = new();
    public List<string> Providers { get; set; } = new();
    public JsonElement? Configuration { get; set; }
}
