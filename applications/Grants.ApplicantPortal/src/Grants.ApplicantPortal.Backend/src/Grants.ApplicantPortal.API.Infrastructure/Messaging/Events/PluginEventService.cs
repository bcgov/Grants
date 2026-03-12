using Grants.ApplicantPortal.API.Core.Plugins;
using Grants.ApplicantPortal.API.Core.Services;
using Grants.ApplicantPortal.API.Infrastructure.Data;
using Grants.ApplicantPortal.API.UseCases;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Grants.ApplicantPortal.API.Infrastructure.Messaging.Events;

/// <summary>
/// Persists plugin events and, for error-severity events, compensates by
/// invalidating the relevant cache segment.
/// Uses <see cref="IPluginCommandMetadataRegistry"/> for cache segment resolution,
/// keeping this service completely plugin-agnostic.
/// </summary>
public class PluginEventService(
    AppDbContext dbContext,
    IProfileCacheInvalidationService cacheInvalidationService,
    IPluginCommandMetadataRegistry metadataRegistry,
    ILogger<PluginEventService> logger) : IPluginEventService
{
  public async Task RecordAsync(PluginEventContext context, CancellationToken cancellationToken = default)
  {
    try
    {
      // 1. Persist the event
      var pluginEvent = new PluginEvent(
          context.ProfileId,
          context.PluginId,
          context.Provider,
          context.DataType,
          context.EntityId,
          context.Severity,
          context.Source,
          context.UserMessage,
          context.TechnicalDetails,
          context.OriginalMessageId,
          context.CorrelationId);

      dbContext.PluginEvents.Add(pluginEvent);
      await dbContext.SaveChangesAsync(cancellationToken);

      logger.LogInformation(
          "Recorded plugin event {EventId} [{Severity}] for ProfileId: {ProfileId}, DataType: {DataType}, Source: {Source}",
          pluginEvent.EventId, context.Severity, context.ProfileId, context.DataType, context.Source);

      // 2. Compensate only for errors — invalidate cache so next read fetches fresh data
      if (context.Severity == PluginEventSeverity.Error)
      {
        var cacheSegment = metadataRegistry.GetCacheSegment(context.PluginId, context.DataType);

        if (cacheSegment != null)
        {
          await cacheInvalidationService.InvalidateProfileDataCacheAsync(
              context.ProfileId,
              context.PluginId,
              context.Provider,
              cacheSegment,
              cancellationToken);

          logger.LogInformation(
              "Compensated error by invalidating cache segment {CacheSegment} for ProfileId: {ProfileId}",
              cacheSegment, context.ProfileId);
        }
      }
    }
    catch (Exception ex)
    {
      // Don't let event recording failures break the caller
      logger.LogError(ex,
          "Failed to record plugin event for ProfileId: {ProfileId}, DataType: {DataType}",
          context.ProfileId, context.DataType);
    }
  }

  public async Task<List<PluginEvent>> GetActiveEventsAsync(
      Guid profileId, string pluginId, string provider, CancellationToken cancellationToken = default)
  {
    return await dbContext.PluginEvents
        .Where(e => e.ProfileId == profileId
                    && e.PluginId == pluginId
                    && e.Provider == provider
                    && !e.IsAcknowledged)
        .OrderByDescending(e => e.CreatedAt)
        .ToListAsync(cancellationToken);
  }

  public async Task<bool> AcknowledgeEventAsync(Guid eventId, Guid profileId, CancellationToken cancellationToken = default)
  {
    var pluginEvent = await dbContext.PluginEvents
        .FirstOrDefaultAsync(e => e.EventId == eventId && e.ProfileId == profileId, cancellationToken);

    if (pluginEvent is null)
      return false;

    pluginEvent.Acknowledge();
    await dbContext.SaveChangesAsync(cancellationToken);

    logger.LogInformation("Acknowledged plugin event {EventId} for ProfileId: {ProfileId}", eventId, profileId);
    return true;
  }

  public async Task AcknowledgeAllAsync(
      Guid profileId, string pluginId, string provider, CancellationToken cancellationToken = default)
  {
    var events = await dbContext.PluginEvents
        .Where(e => e.ProfileId == profileId
                    && e.PluginId == pluginId
                    && e.Provider == provider
                    && !e.IsAcknowledged)
        .ToListAsync(cancellationToken);

    foreach (var e in events)
      e.Acknowledge();

    if (events.Count > 0)
    {
      await dbContext.SaveChangesAsync(cancellationToken);
      logger.LogInformation("Acknowledged {Count} plugin events for ProfileId: {ProfileId}", events.Count, profileId);
    }
  }
}
