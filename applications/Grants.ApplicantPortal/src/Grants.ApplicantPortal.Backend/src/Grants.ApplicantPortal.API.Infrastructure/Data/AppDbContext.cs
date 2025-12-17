using Grants.ApplicantPortal.API.Core.Features.Profiles.ProfileAggregate;
using Grants.ApplicantPortal.API.Infrastructure.Messaging.Outbox;
using Grants.ApplicantPortal.API.Infrastructure.Messaging.Inbox;

namespace Grants.ApplicantPortal.API.Infrastructure.Data;

/// <summary>
/// Application database context
/// </summary>
public class AppDbContext(DbContextOptions<AppDbContext> options,
  IDomainEventDispatcher? dispatcher) : DbContext(options)
{ 
  // Add your DbSets here
  public DbSet<Profile> Profiles => Set<Profile>();

  // Messaging entities
  public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
  public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    base.OnModelCreating(modelBuilder);
    modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
  }

  public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = new CancellationToken())
  {
    int result = await base.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

    // ignore events if no dispatcher provided
    if (dispatcher == null) return result;

    // dispatch events only if save was successful
    var entitiesWithEvents = ChangeTracker.Entries<HasDomainEventsBase>()
        .Select(e => e.Entity)
        .Where(e => e.DomainEvents.Any())
        .ToArray();

    await dispatcher.DispatchAndClearEvents(entitiesWithEvents);

    return result;
  }

  public override int SaveChanges() =>
        SaveChangesAsync().GetAwaiter().GetResult();
}
