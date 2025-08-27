namespace Grants.ApplicantPortal.API.Core.Features.PluginConfigurations.PluginConfigurationAggregate.Specifications;

public class PluginConfigurationByPluginIdSpec : Specification<PluginConfiguration>
{
  public PluginConfigurationByPluginIdSpec(string pluginId) =>
    Query
        .Where(pluginConfiguration => pluginConfiguration.PluginId == pluginId);
}
