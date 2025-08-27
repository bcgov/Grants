using Grants.ApplicantPortal.API.Core.Features.PluginConfigurations.PluginConfigurationAggregate;

namespace Grants.ApplicantPortal.API.Infrastructure.Data.Config;

/// <summary>
/// Entity Framework configuration for PluginConfiguration entity
/// </summary>
public class PluginConfigurationConfiguration : IEntityTypeConfiguration<PluginConfiguration>
{
    public void Configure(EntityTypeBuilder<PluginConfiguration> builder)
    {
        builder.ToTable("PluginConfigurations");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.PluginId)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(p => p.ConfigurationJson)
            .IsRequired()
            .HasColumnType("jsonb"); // PostgreSQL JSON binary column type for better performance
    }
}
