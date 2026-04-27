using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Grants.ApplicantPortal.API.Infrastructure.Messaging.Configuration;

public static class RedisConnectionOptionsFactory
{
    public static ConfigurationOptions Create(string connectionString, ILogger logger, string purpose)
    {
        var options = ConfigurationOptions.Parse(connectionString);

        options.AbortOnConnectFail = false;
        options.ConnectRetry = Math.Max(options.ConnectRetry, 5);
        options.ConnectTimeout = Math.Max(options.ConnectTimeout, 10000);
        options.SyncTimeout = Math.Max(options.SyncTimeout, 5000);
        options.ReconnectRetryPolicy = new ExponentialRetry(3000);

        logger.LogInformation(
            "Redis options prepared for {Purpose}. Sentinel: {IsSentinel}, ServiceName: {ServiceName}, Endpoints: {Endpoints}",
            purpose,
            !string.IsNullOrWhiteSpace(options.ServiceName),
            string.IsNullOrWhiteSpace(options.ServiceName) ? "(none)" : options.ServiceName,
            string.Join(", ", options.EndPoints.Select(ep => ep.ToString())));

        return options;
    }
}
