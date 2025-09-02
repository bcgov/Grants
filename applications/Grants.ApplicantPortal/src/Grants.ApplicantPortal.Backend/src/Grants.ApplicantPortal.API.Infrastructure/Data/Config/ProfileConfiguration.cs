using Grants.ApplicantPortal.API.Core.Features.Profiles.ProfileAggregate;

namespace Grants.ApplicantPortal.API.Infrastructure.Data.Config;

/// <summary>
/// Entity Framework configuration for Profile entity
/// </summary>
public class ProfileConfiguration : IEntityTypeConfiguration<Profile>
{
  public void Configure(EntityTypeBuilder<Profile> builder)
  {
    builder.Property(p => p.Subject)
      .IsRequired()
      .HasMaxLength(256);

    builder.HasIndex(p => p.Subject)
      .IsUnique();
  }
}
