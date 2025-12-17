namespace Grants.ApplicantPortal.API.Infrastructure.Messaging.RabbitMQ;

/// <summary>
/// Configuration for RabbitMQ connection and message publishing
/// </summary>
public class RabbitMQConfiguration
{
    /// <summary>
    /// RabbitMQ host name or IP address
    /// </summary>
    public string HostName { get; set; } = "localhost";

    /// <summary>
    /// RabbitMQ port (default 5672 for non-SSL, 5671 for SSL)
    /// </summary>
    public int Port { get; set; } = 5672;

    /// <summary>
    /// Username for RabbitMQ authentication
    /// </summary>
    public string UserName { get; set; } = "guest";

    /// <summary>
    /// Password for RabbitMQ authentication
    /// </summary>
    public string Password { get; set; } = "guest";

    /// <summary>
    /// Virtual host (default is "/")
    /// </summary>
    public string VirtualHost { get; set; } = "/";

    /// <summary>
    /// Enable SSL/TLS connection
    /// </summary>
    public bool UseSsl { get; set; } = false;

    /// <summary>
    /// Connection timeout in seconds
    /// </summary>
    public int ConnectionTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Number of retry attempts for failed operations
    /// </summary>
    public int RetryCount { get; set; } = 3;

    /// <summary>
    /// Delay between retry attempts
    /// </summary>
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Default exchange name for publishing messages
    /// </summary>
    public string DefaultExchange { get; set; } = "grants.messaging";

    /// <summary>
    /// Exchange type (direct, topic, fanout, headers)
    /// </summary>
    public string ExchangeType { get; set; } = "topic";

    /// <summary>
    /// Whether to declare the exchange if it doesn't exist
    /// </summary>
    public bool DeclareExchange { get; set; } = true;

    /// <summary>
    /// Whether the exchange should be durable
    /// </summary>
    public bool ExchangeDurable { get; set; } = true;

    /// <summary>
    /// Default queue name for consuming messages
    /// </summary>
    public string DefaultQueue { get; set; } = "grants.messaging.inbox";

    /// <summary>
    /// Whether to declare the queue if it doesn't exist
    /// </summary>
    public bool DeclareQueue { get; set; } = true;

    /// <summary>
    /// Whether the queue should be durable
    /// </summary>
    public bool QueueDurable { get; set; } = true;

    /// <summary>
    /// Whether the queue should auto-delete when no consumers
    /// </summary>
    public bool QueueAutoDelete { get; set; } = false;

    /// <summary>
    /// Maximum message size in bytes
    /// </summary>
    public int MaxMessageSize { get; set; } = 1024 * 1024; // 1MB

    /// <summary>
    /// Publisher confirm timeout
    /// </summary>
    public TimeSpan PublisherConfirmTimeout { get; set; } = TimeSpan.FromSeconds(10);
}
