namespace Grants.ApplicantPortal.API.Core.Features.PluginConfigurations.PluginConfigurationAggregate.Events;

/// <summary>
/// A domain event that is dispatched whenever a plugin configuration is reset.
/// The PluginConfigurationService is used to dispatch this event.
/// </summary>
internal sealed class PluginConfigurationResetEvent(int pluginId) : DomainEventBase
{
  public int PluginId { get; init; } = pluginId;
}
