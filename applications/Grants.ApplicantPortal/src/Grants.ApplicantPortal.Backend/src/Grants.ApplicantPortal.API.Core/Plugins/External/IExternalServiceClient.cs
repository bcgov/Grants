namespace Grants.ApplicantPortal.API.Core.Plugins.External;

/// <summary>
/// Configuration model for external service endpoints
/// </summary>
public class ExternalServiceConfiguration
{
    public required string BaseUrl { get; set; }
    public string? ApiKey { get; set; }
    public Dictionary<string, string>? Headers { get; set; }
    public int TimeoutSeconds { get; set; } = 30;
    public int MaxRetryAttempts { get; set; } = 3;
    public bool EnableCircuitBreaker { get; set; } = true;
}

/// <summary>
/// Request model for external service calls
/// </summary>
public class ExternalServiceRequest
{
    public required string Endpoint { get; set; }
    public HttpMethod Method { get; set; } = HttpMethod.Get;
    public object? Body { get; set; }
    public Dictionary<string, string>? Headers { get; set; }
    public Dictionary<string, string>? QueryParameters { get; set; }
}

/// <summary>
/// Response model from external service calls
/// </summary>
public class ExternalServiceResponse<T>
{
    public bool IsSuccess { get; set; }
    public T? Data { get; set; }
    public string? ErrorMessage { get; set; }
    public int StatusCode { get; set; }
    public Dictionary<string, string>? Headers { get; set; }
}

/// <summary>
/// Interface for making resilient external service calls
/// </summary>
public interface IExternalServiceClient
{
    /// <summary>
    /// Make a resilient HTTP call to an external service
    /// </summary>
    Task<ExternalServiceResponse<T>> CallAsync<T>(
        string pluginId,
        ExternalServiceRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Make a resilient HTTP call returning raw string response
    /// </summary>
    Task<ExternalServiceResponse<string>> CallAsync(
        string pluginId,
        ExternalServiceRequest request,
        CancellationToken cancellationToken = default);
}
