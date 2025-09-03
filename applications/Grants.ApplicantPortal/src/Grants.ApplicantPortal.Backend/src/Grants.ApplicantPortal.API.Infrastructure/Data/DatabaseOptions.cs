namespace Grants.ApplicantPortal.API.Infrastructure.Data;

public class DatabaseOptions
{
    public const string SectionName = "Database";
    
    public bool RunMigrationsOnStartup { get; set; } = false;
    public bool SeedDataOnStartup { get; set; } = false;
}
