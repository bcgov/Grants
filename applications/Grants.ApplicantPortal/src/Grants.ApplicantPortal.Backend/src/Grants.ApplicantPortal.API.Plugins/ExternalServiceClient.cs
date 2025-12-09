using System.Text;
using System.Text.Json;
using System.Net.Http.Headers;
using Microsoft.Extensions.Configuration;
using Grants.ApplicantPortal.API.Core;

namespace Grants.ApplicantPortal.API.Plugins;

/// <summary>
/// Implementation of external service client with resilience patterns
/// </summary>
public class ExternalServiceClient : IExternalServiceClient
{
  private readonly HttpClient _httpClient;
  private readonly IConfiguration _configuration;
  private readonly ILogger<ExternalServiceClient> _logger;
  private readonly JsonSerializerOptions _jsonOptions;

  public ExternalServiceClient(
      HttpClient httpClient,
      IConfiguration configuration,
      ILogger<ExternalServiceClient> logger)
  {
    _httpClient = httpClient;
    _configuration = configuration;
    _logger = logger;
    _jsonOptions = new JsonSerializerOptions
    {
      PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
      WriteIndented = false
    };
  }

  public async Task<ExternalServiceResponse<T>> CallAsync<T>(
      string pluginId,
      ExternalServiceRequest request,
      CancellationToken cancellationToken = default)
  {
    var stringResponse = await CallAsync(pluginId, request, cancellationToken);

    if (!stringResponse.IsSuccess)
    {
      return new ExternalServiceResponse<T>
      {
        IsSuccess = false,
        ErrorMessage = stringResponse.ErrorMessage,
        StatusCode = stringResponse.StatusCode,
        Headers = stringResponse.Headers
      };
    }

    try
    {
      var data = JsonSerializer.Deserialize<T>(stringResponse.Data!, _jsonOptions);
      return new ExternalServiceResponse<T>
      {
        IsSuccess = true,
        Data = data,
        StatusCode = stringResponse.StatusCode,
        Headers = stringResponse.Headers
      };
    }
    catch (JsonException ex)
    {
      _logger.LogError(ex, "Failed to deserialize response for plugin {PluginId}: {Error}", pluginId, ex.Message);
      return new ExternalServiceResponse<T>
      {
        IsSuccess = false,
        ErrorMessage = $"Failed to deserialize response: {ex.Message}",
        StatusCode = stringResponse.StatusCode,
        Headers = stringResponse.Headers
      };
    }
  }

  public async Task<ExternalServiceResponse<string>> CallAsync(
      string pluginId,
      ExternalServiceRequest request,
      CancellationToken cancellationToken = default)
  {
    try
    {
      _logger.LogInformation("Making external service call for plugin {PluginId} to endpoint {Endpoint}",
          pluginId, request.Endpoint);

      // Get plugin configuration
      var config = GetPluginConfiguration(pluginId);
      if (config == null)
      {
        return new ExternalServiceResponse<string>
        {
          IsSuccess = false,
          ErrorMessage = $"Configuration not found for plugin {pluginId}",
          StatusCode = 500
        };
      }

      // Build the full URL
      var fullUrl = BuildFullUrl(config.BaseUrl, request.Endpoint, request.QueryParameters);

      // Create HTTP request message
      using var httpRequest = new HttpRequestMessage(request.Method, fullUrl);

      // Add headers from configuration
      AddHeaders(httpRequest, config, request);

      // Add body if present
      if (request.Body != null && (request.Method == HttpMethod.Post || request.Method == HttpMethod.Put))
      {
        var jsonBody = JsonSerializer.Serialize(request.Body, _jsonOptions);
        httpRequest.Content = new StringContent(jsonBody, Encoding.UTF8, MediaTypeHeaderValue.Parse("application/json"));
      }

      // Set timeout
      using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
      timeoutCts.CancelAfter(TimeSpan.FromSeconds(config.TimeoutSeconds));

      // Make the HTTP call (resilience is configured at the HttpClient level)
      using var response = await _httpClient.SendAsync(httpRequest, timeoutCts.Token);

      // Read response content
      var content = await response.Content.ReadAsStringAsync(cancellationToken);

      // Extract response headers
      var responseHeaders = response.Headers
          .Concat(response.Content.Headers)
          .ToDictionary(h => h.Key, h => string.Join(", ", h.Value));

      var result = new ExternalServiceResponse<string>
      {
        IsSuccess = response.IsSuccessStatusCode,
        Data = content,
        StatusCode = (int)response.StatusCode,
        Headers = responseHeaders
      };

      if (!response.IsSuccessStatusCode)
      {
        result.ErrorMessage = $"HTTP {response.StatusCode}: {response.ReasonPhrase}";
        _logger.LogWarning("External service call failed for plugin {PluginId}: {StatusCode} {ReasonPhrase}",
            pluginId, response.StatusCode, response.ReasonPhrase);
      }
      else
      {
        _logger.LogInformation("External service call succeeded for plugin {PluginId}: {StatusCode}",
            pluginId, response.StatusCode);
      }

      return result;
    }
    catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
    {
      _logger.LogError(ex, "Timeout occurred during external service call for plugin {PluginId}", pluginId);
      return new ExternalServiceResponse<string>
      {
        IsSuccess = false,
        ErrorMessage = "Request timeout",
        StatusCode = 408
      };
    }
    catch (HttpRequestException ex)
    {
      _logger.LogError(ex, "HTTP request exception during external service call for plugin {PluginId}: {Error}",
          pluginId, ex.Message);
      return new ExternalServiceResponse<string>
      {
        IsSuccess = false,
        ErrorMessage = $"HTTP request failed: {ex.Message}",
        StatusCode = 500
      };
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Unexpected error during external service call for plugin {PluginId}: {Error}",
          pluginId, ex.Message);
      return new ExternalServiceResponse<string>
      {
        IsSuccess = false,
        ErrorMessage = $"Unexpected error: {ex.Message}",
        StatusCode = 500
      };
    }
  }

  private ExternalServiceConfiguration? GetPluginConfiguration(string pluginId)
  {
    try
    {
      // Get plugin configuration from app settings under "Plugins:{pluginId}"
      var configSection = _configuration.GetSection($"Plugins:{pluginId}");
      
      if (!configSection.Exists())
      {
        _logger.LogWarning("Plugin configuration not found for {PluginId} in app settings", pluginId);
        return null;
      }

      // Map configuration section to ExternalServiceConfiguration
      var config = new ExternalServiceConfiguration
      {
        BaseUrl = configSection["BaseUrl"] ?? configSection["Endpoint"] ?? string.Empty,
        ApiKey = configSection["ApiKey"],
        TimeoutSeconds = configSection.GetValue<int>("TimeoutSeconds", 30),
        MaxRetryAttempts = configSection.GetValue<int>("MaxRetryAttempts", 3),
        EnableCircuitBreaker = configSection.GetValue<bool>("EnableCircuitBreaker", true)
      };

      // Get headers if configured
      var headersSection = configSection.GetSection("Headers");
      if (headersSection.Exists())
      {
        config.Headers = new Dictionary<string, string>();
        foreach (var header in headersSection.GetChildren())
        {
          config.Headers[header.Key] = header.Value ?? string.Empty;
        }
      }

      // Validate required fields
      if (string.IsNullOrEmpty(config.BaseUrl))
      {
        _logger.LogError("BaseUrl is required for plugin {PluginId} configuration", pluginId);
        return null;
      }

      _logger.LogDebug("Successfully loaded configuration for plugin {PluginId}: BaseUrl={BaseUrl}, TimeoutSeconds={TimeoutSeconds}", 
        pluginId, config.BaseUrl, config.TimeoutSeconds);

      return config;
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Failed to load configuration for plugin {PluginId}: {Error}", pluginId, ex.Message);
      return null;
    }
  }

  private static string BuildFullUrl(string baseUrl, string endpoint, Dictionary<string, string>? queryParameters)
  {
    var baseUri = new Uri(baseUrl.TrimEnd('/'));
    var endpointUri = new Uri(baseUri, endpoint.TrimStart('/'));

    if (queryParameters?.Any() == true)
    {
      var query = string.Join("&", queryParameters.Select(kvp =>
          $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"));

      var uriBuilder = new UriBuilder(endpointUri)
      {
        Query = string.IsNullOrEmpty(endpointUri.Query) ? query : $"{endpointUri.Query.TrimStart('?')}&{query}"
      };

      return uriBuilder.ToString();
    }

    return endpointUri.ToString();
  }

  private static void AddHeaders(HttpRequestMessage request, ExternalServiceConfiguration config, ExternalServiceRequest serviceRequest)
  {
    // Add API key if present
    if (!string.IsNullOrEmpty(config.ApiKey))
    {
      request.Headers.Add("Authorization", $"Bearer {config.ApiKey}");
    }

    // Add headers from configuration
    if (config.Headers?.Any() == true)
    {
      foreach (var header in config.Headers)
      {
        request.Headers.TryAddWithoutValidation(header.Key, header.Value);
      }
    }

    // Add headers from request
    if (serviceRequest.Headers?.Any() == true)
    {
      foreach (var header in serviceRequest.Headers)
      {
        request.Headers.TryAddWithoutValidation(header.Key, header.Value);
      }
    }
  }
}
