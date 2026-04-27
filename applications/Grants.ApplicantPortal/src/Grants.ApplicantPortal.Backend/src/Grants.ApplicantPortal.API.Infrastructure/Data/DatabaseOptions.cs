namespace Grants.ApplicantPortal.API.Infrastructure.Data;

public class DatabaseOptions
{
    public const string SectionName = "Database";

    public bool RunMigrationsOnStartup { get; set; } = false;
    public bool SeedDataOnStartup { get; set; } = false;

    public int CommandTimeoutSeconds { get; set; } = 30;
    public int MaxRetryCount { get; set; } = 5;
    public int MaxRetryDelaySeconds { get; set; } = 10;
}
