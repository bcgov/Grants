using Grants.ApplicantPortal.API.Infrastructure.Messaging.Inbox;

namespace Grants.ApplicantPortal.API.Infrastructure.Data.Config;

public class InboxMessageConfiguration : IEntityTypeConfiguration<InboxMessage>
{
    public void Configure(EntityTypeBuilder<InboxMessage> builder)
    {
        builder.ToTable("InboxMessages", DataSchemaConstants.DEFAULT_SCHEMA);

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .UseIdentityColumn();

        builder.Property(x => x.MessageId)
            .IsRequired()
            .HasColumnName("MessageId");

        builder.Property(x => x.MessageType)
            .IsRequired()
            .HasMaxLength(255)
            .HasColumnName("MessageType");

        builder.Property(x => x.Payload)
            .IsRequired()
            .HasColumnName("Payload");

        builder.Property(x => x.ReceivedAt)
            .IsRequired()
            .HasColumnName("ReceivedAt");

        builder.Property(x => x.ProcessedAt)
            .HasColumnName("ProcessedAt");

        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion<int>()
            .HasColumnName("Status");

        builder.Property(x => x.RetryCount)
            .IsRequired()
            .HasDefaultValue(0)
            .HasColumnName("RetryCount");

        builder.Property(x => x.LastError)
            .HasMaxLength(4000) // Allow for detailed error messages
            .HasColumnName("LastError");

        builder.Property(x => x.CorrelationId)
            .HasMaxLength(255)
            .HasColumnName("CorrelationId");

        builder.Property(x => x.LockToken)
            .HasMaxLength(255)
            .HasColumnName("LockToken");

        builder.Property(x => x.LockExpiry)
            .HasColumnName("LockExpiry");

        // Indexes for performance
        builder.HasIndex(x => x.MessageId)
            .IsUnique()
            .HasDatabaseName("IX_InboxMessages_MessageId");
    }
}
