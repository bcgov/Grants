namespace Grants.ApplicantPortal.API.Infrastructure.Messaging.Configuration;

/// <summary>
/// Configuration options for the messaging system
/// </summary>
public class MessagingOptions
{
    public const string SectionName = "Messaging";

    /// <summary>
    /// RabbitMQ configuration
    /// </summary>
    public RabbitMQOptions RabbitMQ { get; set; } = new();

    /// <summary>
    /// Outbox configuration
    /// </summary>
    public OutboxOptions Outbox { get; set; } = new();

    /// <summary>
    /// Inbox configuration
    /// </summary>
    public InboxOptions Inbox { get; set; } = new();

    /// <summary>
    /// Distributed locking configuration
    /// </summary>
    public DistributedLockOptions DistributedLocks { get; set; } = new();

    /// <summary>
    /// Background job configuration
    /// </summary>
    public BackgroundJobOptions BackgroundJobs { get; set; } = new();
}

/// <summary>
/// RabbitMQ connection and configuration options
/// </summary>
public class RabbitMQOptions
{
    /// <summary>
    /// RabbitMQ hostname
    /// </summary>
    public string HostName { get; set; } = "localhost";

    /// <summary>
    /// RabbitMQ port
    /// </summary>
    public int Port { get; set; } = 5672;

    /// <summary>
    /// RabbitMQ username
    /// </summary>
    public string UserName { get; set; } = "guest";

    /// <summary>
    /// RabbitMQ password
    /// </summary>
    public string Password { get; set; } = "guest";

    /// <summary>
    /// RabbitMQ virtual host
    /// </summary>
    public string VirtualHost { get; set; } = "/";

    /// <summary>
    /// Number of retry attempts for failed operations
    /// </summary>
    public int RetryCount { get; set; } = 3;

    /// <summary>
    /// Delay between retry attempts
    /// </summary>
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Connection timeout
    /// </summary>
    public TimeSpan ConnectionTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Enable SSL/TLS
    /// </summary>
    public bool UseSsl { get; set; } = false;
}

/// <summary>
/// Outbox processing configuration
/// </summary>
public class OutboxOptions
{
    /// <summary>
    /// How often to poll for new messages (in seconds)
    /// </summary>
    public int PollingIntervalSeconds { get; set; } = 30;

    /// <summary>
    /// Number of messages to process in each batch
    /// </summary>
    public int BatchSize { get; set; } = 100;

    /// <summary>
    /// Maximum number of retry attempts for failed messages
    /// </summary>
    public int MaxRetries { get; set; } = 5;

    /// <summary>
    /// How long to keep processed messages before cleanup (in days)
    /// </summary>
    public int RetentionDays { get; set; } = 7;

    /// <summary>
    /// How often to run cleanup job (in hours)
    /// </summary>
    public int CleanupIntervalHours { get; set; } = 24;
}

/// <summary>
/// Inbox processing configuration
/// </summary>
 public class InboxOptions
{
    /// <summary>
    /// How often to poll for new messages (in seconds)
    /// </summary>
    public int PollingIntervalSeconds { get; set; } = 15;

    /// <summary>
    /// Number of messages to process in each batch
    /// </summary>
    public int BatchSize { get; set; } = 50;

    /// <summary>
    /// Maximum number of retry attempts for failed messages
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// How long to keep processed messages before cleanup (in days)
    /// </summary>
    public int RetentionDays { get; set; } = 7;

    /// <summary>
    /// How often to run cleanup job (in hours)
    /// </summary>
    public int CleanupIntervalHours { get; set; } = 24;
}

/// <summary>
/// Distributed locking configuration
/// Note: Locking implementation is automatically selected based on Redis configuration:
/// - If Redis connection string is present: Uses Redis-based distributed locking
/// - If no Redis connection string: Uses in-memory distributed locking (suitable for single-pod deployments)
/// </summary>
public class DistributedLockOptions
{
    /// <summary>
    /// Default timeout for acquiring locks (in minutes)
    /// </summary>
    public int DefaultTimeoutMinutes { get; set; } = 5;

    /// <summary>
    /// How often to renew locks during long-running operations (in minutes)
    /// </summary>
    public int RenewalIntervalMinutes { get; set; } = 2;

    /// <summary>
    /// How long to wait when trying to acquire a lock (in seconds)
    /// </summary>
    public int WaitTimeoutSeconds { get; set; } = 5;
}

/// <summary>
/// Background job processing configuration
/// </summary>
public class BackgroundJobOptions
{
    /// <summary>
    /// Maximum number of concurrent jobs
    /// </summary>
    public int MaxConcurrency { get; set; } = Environment.ProcessorCount;

    /// <summary>
    /// Enable or disable background job processing
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Misfire threshold for Quartz jobs (in seconds)
    /// </summary>
    public int MisfireThresholdSeconds { get; set; } = 60;

    /// <summary>
    /// Base interval (in seconds) for the first backoff step when a job fails.
    /// Subsequent failures increase the delay exponentially.
    /// </summary>
    public int BaseBackoffSeconds { get; set; } = 15;

    /// <summary>
    /// Maximum backoff interval (in seconds) between retry attempts.
    /// The exponential delay is capped at this value (default: 5 minutes).
    /// </summary>
    public int MaxBackoffSeconds { get; set; } = 300;

    /// <summary>
    /// Multiplier applied for each consecutive failure.
    /// Delay = BaseBackoffSeconds * BackoffMultiplier^(failures-1).
    /// </summary>
    public double BackoffMultiplier { get; set; } = 2.0;

    /// <summary>
    /// During sustained failures, emit a full warning log every Nth failure
    /// to provide periodic visibility without flooding logs.
    /// </summary>
    public int LogEveryNthFailure { get; set; } = 20;
}
