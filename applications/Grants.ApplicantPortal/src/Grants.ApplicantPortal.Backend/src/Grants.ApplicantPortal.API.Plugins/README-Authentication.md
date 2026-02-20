# Plugin HTTP Authentication Guide

## Overview

The `ExternalServiceClient` provides a generic, reusable HTTP client wrapper for making authenticated calls to external plugin endpoints. It supports multiple authentication schemes including Bearer tokens and custom API key headers.

## Authentication Schemes

### 1. Bearer Token Authentication (Default)

For services that use OAuth 2.0 or similar Bearer token authentication:

```json
{
  "Plugins": {
    "YOUR_PLUGIN": {
      "Configuration": {
        "BaseUrl": "https://api.example.com",
        "ApiKey": "your-secret-token",
        "AuthHeaderName": "Bearer"  // This is the default
      }
    }
  }
}
```

This will add the header: `Authorization: Bearer your-secret-token`

### 2. Custom Header Authentication (e.g., X-Api-Key)

For services that require API keys in custom headers (like Unity):

```json
{
  "Plugins": {
    "UNITY": {
      "Configuration": {
        "BaseUrl": "http://localhost:5555",
        "ApiKey": "dev-unity-api-key",
        "AuthHeaderName": "X-Api-Key"
      }
    }
  }
}
```

This will add the header: `X-Api-Key: dev-unity-api-key`

### 3. Other Custom Headers

You can use any header name:

```json
{
  "Configuration": {
    "ApiKey": "my-secret-key",
    "AuthHeaderName": "ApiKey"        // or
    "AuthHeaderName": "X-Custom-Auth" // or any other header name
  }
}
```

## Usage in Plugin Code

The authentication is handled automatically by the `ExternalServiceClient`. Simply use it in your plugin:

```csharp
public class MyPlugin : IProfilePlugin
{
    private readonly IExternalServiceClient _externalServiceClient;

    public MyPlugin(IExternalServiceClient externalServiceClient)
    {
        _externalServiceClient = externalServiceClient;
    }

    public async Task<ProfileData> GetProfileAsync(string userId)
    {
        var request = new ExternalServiceRequest
        {
            Endpoint = $"/api/users/{userId}",
            Method = HttpMethod.Get
        };

        // The API key and authentication headers are added automatically
        var response = await _externalServiceClient.CallAsync<ProfileData>(
            pluginId: PluginId,
            request: request
        );

        return response.Data;
    }
}
```

## Configuration Reference

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `BaseUrl` | string | *required* | Base URL of the external service |
| `ApiKey` | string | null | API key or token for authentication |
| `AuthHeaderName` | string | "Bearer" | Header name to use for the API key. Use "Bearer" for `Authorization: Bearer {token}` or any custom header name like "X-Api-Key" |
| `TimeoutSeconds` | int | 30 | Request timeout in seconds |
| `MaxRetryAttempts` | int | 3 | Number of retry attempts for failed requests |
| `EnableCircuitBreaker` | bool | true | Enable circuit breaker pattern |
| `Headers` | object | null | Additional static headers to include in all requests |

## Additional Headers

You can add additional static headers that will be sent with every request:

```json
{
  "Configuration": {
    "BaseUrl": "https://api.example.com",
    "ApiKey": "your-key",
    "AuthHeaderName": "X-Api-Key",
    "Headers": {
      "User-Agent": "Grants-ApplicantPortal/1.0",
      "Accept": "application/json",
      "X-Custom-Header": "custom-value"
    }
  }
}
```

## Per-Request Headers

You can also add headers for specific requests:

```csharp
var request = new ExternalServiceRequest
{
    Endpoint = "/api/resource",
    Method = HttpMethod.Post,
    Headers = new Dictionary<string, string>
    {
        { "X-Request-Id", Guid.NewGuid().ToString() }
    }
};
```

## Resilience Features

The `ExternalServiceClient` includes built-in resilience patterns:

- **Retry Policy**: Automatically retries transient failures (configurable)
- **Circuit Breaker**: Prevents cascading failures by opening the circuit after a threshold of failures
- **Timeout**: Enforces request timeouts to prevent hanging
- **Exponential Backoff**: Increases delay between retry attempts

## Examples

### Unity Plugin (X-Api-Key Authentication)
```json
{
  "UNITY": {
    "Configuration": {
      "BaseUrl": "http://localhost:5555",
      "ApiKey": "dev-unity-api-key",
      "AuthHeaderName": "X-Api-Key"
    }
  }
}
```

### Demo Plugin (Bearer Token Authentication)
```json
{
  "DEMO": {
    "Configuration": {
      "BaseUrl": "https://demo-plugin-endpoint.com",
      "ApiKey": "your-demo-api-key",
      "AuthHeaderName": "Bearer"
    }
  }
}
```

### Custom Authentication Scheme
```json
{
  "CUSTOM": {
    "Configuration": {
      "BaseUrl": "https://custom-api.com",
      "ApiKey": "secret-key",
      "AuthHeaderName": "X-Custom-Auth-Token"
    }
  }
}
```
