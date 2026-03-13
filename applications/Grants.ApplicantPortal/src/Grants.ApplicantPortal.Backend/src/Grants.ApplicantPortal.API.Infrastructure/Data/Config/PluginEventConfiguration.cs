using Grants.ApplicantPortal.API.Core.Plugins;

namespace Grants.ApplicantPortal.API.Infrastructure.Data.Config;

public class PluginEventConfiguration : IEntityTypeConfiguration<PluginEvent>
{
  public void Configure(EntityTypeBuilder<PluginEvent> builder)
  {
    builder.ToTable("PluginEvents", DataSchemaConstants.DEFAULT_SCHEMA);

    builder.HasKey(x => x.Id);

    builder.Property(x => x.Id)
        .UseIdentityColumn();

    builder.Property(x => x.EventId)
        .IsRequired();

    builder.Property(x => x.ProfileId)
        .IsRequired();

    builder.Property(x => x.PluginId)
        .IsRequired()
        .HasMaxLength(50);

    builder.Property(x => x.Provider)
        .IsRequired()
        .HasMaxLength(50);

    builder.Property(x => x.DataType)
        .IsRequired()
        .HasMaxLength(100);

    builder.Property(x => x.EntityId)
        .HasMaxLength(255);

    builder.Property(x => x.Severity)
        .IsRequired()
        .HasConversion<int>();

    builder.Property(x => x.Source)
        .IsRequired()
        .HasConversion<int>();

    builder.Property(x => x.UserMessage)
        .IsRequired()
        .HasMaxLength(1000);

    builder.Property(x => x.TechnicalDetails)
        .HasMaxLength(4000);

    builder.Property(x => x.OriginalMessageId);

    builder.Property(x => x.CorrelationId)
        .HasMaxLength(255);

    builder.Property(x => x.IsAcknowledged)
        .IsRequired()
        .HasDefaultValue(false);

    builder.Property(x => x.CreatedAt)
        .IsRequired();

    builder.Property(x => x.AcknowledgedAt);

    // Indexes for common query patterns
    builder.HasIndex(x => x.EventId)
        .IsUnique();

    builder.HasIndex(x => new { x.ProfileId, x.PluginId, x.Provider, x.IsAcknowledged })
        .HasDatabaseName("IX_PluginEvents_Profile_Active");
  }
}
