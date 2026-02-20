using Grants.ApplicantPortal.API.Core.Features.Security.SecurityLogAggregate;

namespace Grants.ApplicantPortal.API.Infrastructure.Data.Config;

/// <summary>
/// Entity Framework configuration for SecurityLog entity
/// </summary>
public class SecurityLogConfiguration : IEntityTypeConfiguration<SecurityLog>
{
    public void Configure(EntityTypeBuilder<SecurityLog> builder)
    {
        builder.Property(s => s.ProfileId)
            .IsRequired();

        builder.Property(s => s.EventType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(s => s.EventDescription)
            .HasMaxLength(1000);

        builder.Property(s => s.IpAddress)
            .HasMaxLength(45); // IPv6 max length

        builder.Property(s => s.UserAgent)
            .HasMaxLength(2000);

        builder.Property(s => s.SessionId)
            .HasMaxLength(256);

        builder.Property(s => s.ErrorMessage)
            .HasMaxLength(2000);
    }
}
