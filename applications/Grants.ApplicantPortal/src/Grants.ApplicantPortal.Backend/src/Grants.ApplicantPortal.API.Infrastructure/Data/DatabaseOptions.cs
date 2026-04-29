namespace Grants.ApplicantPortal.API.Infrastructure.Data;

public class DatabaseOptions
{
    public const string SectionName = "Database";

    public bool RunMigrationsOnStartup { get; set; } = false;
    public bool SeedDataOnStartup { get; set; } = false;

    public int CommandTimeoutSeconds { get; set; } = 30;
    public int MaxRetryCount { get; set; } = 5;
    public int MaxRetryDelaySeconds { get; set; } = 10;

    /// <summary>
    /// Maximum lifetime (seconds) of a pooled connection before it is closed and replaced.
    /// Prevents stale connections surviving a CrunchyDB / Postgres pod recycle overnight.
    /// Default: 300 seconds (5 minutes).
    /// </summary>
    public int ConnectionLifetimeSeconds { get; set; } = 300;

    /// <summary>
    /// How long (seconds) an idle connection may remain in the pool before being pruned.
    /// Default: 60 seconds.
    /// </summary>
    public int ConnectionIdleLifetimeSeconds { get; set; } = 60;
}
