using System.Text.Json;
using Grants.ApplicantPortal.API.Web.Configurations;
using System.Net.Http.Headers;
using Microsoft.Extensions.Options;

namespace Grants.ApplicantPortal.API.Web.Auth;

/// <summary>
/// OAuth callback endpoint for automated token retrieval
/// This endpoint captures the authorization code and exchanges it for JWT tokens
/// </summary>
public class AuthCallback : EndpointWithoutRequest
{
    private readonly IOptions<KeycloakConfiguration> _keycloakOptions;

    public AuthCallback(IOptions<KeycloakConfiguration> keycloakOptions)
    {
        _keycloakOptions = keycloakOptions;
    }

    public override void Configure()
    {
        Get("/auth/callback");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "OAuth callback endpoint";
            s.Description = "Handles OAuth authorization code callback and exchanges it for JWT tokens automatically";
            s.Responses[200] = "Authorization code processed successfully - returns HTML page or JSON";
            s.Responses[400] = "Bad request - missing or invalid authorization code";
            s.Responses[500] = "Internal server error during token exchange";
        });
        
        Tags("Authentication", "OAuth");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var code = Query<string>("code", isRequired: false);
        var state = Query<string>("state", isRequired: false);
        var error = Query<string>("error", isRequired: false);
        var errorDescription = Query<string>("error_description", isRequired: false);
        var acceptHeader = HttpContext.Request.Headers.Accept.ToString();
        
        // Improved JSON request detection - explicit Accept header for JSON
        var isJsonRequest = acceptHeader.Contains("application/json") && 
                           !acceptHeader.Contains("text/html") && 
                           !string.IsNullOrEmpty(acceptHeader) &&
                           acceptHeader != "*/*";
        
        Logger.LogInformation("Callback request - Code: {CodePresent}, Error: {Error}, Accept: {Accept}, IsJson: {IsJson}", 
            !string.IsNullOrEmpty(code), error, acceptHeader, isJsonRequest);
        
        // Check for OAuth errors first
        if (!string.IsNullOrEmpty(error))
        {
            var errorMessage = $"OAuth Error: {error}";
            if (!string.IsNullOrEmpty(errorDescription))
            {
                errorMessage += $" - {errorDescription}";
            }
            
            if (isJsonRequest)
            {
                var errorResponse = new AuthCallbackResponse(
                    Success: false,
                    AuthorizationCode: null,
                    State: state,
                    AccessToken: null,
                    RefreshToken: null,
                    ExpiresIn: 0,
                    TokenType: null,
                    Scope: null,
                    UserInfo: null,
                    Message: errorMessage,
                    Timestamp: DateTime.UtcNow
                );
                
                await SendAsync(errorResponse, cancellation: ct);
                return;
            }
            else
            {
                // Serve HTML page with error
                await ServeAuthSuccessPage(null, error, errorDescription, ct);
                return;
            }
        }
        
        // If no code is provided, serve the HTML page (user likely navigated directly)
        if (string.IsNullOrEmpty(code))
        {
            if (isJsonRequest)
            {
                await SendErrorsAsync(cancellation: ct);
                AddError("No authorization code received");
                return;
            }
            else
            {
                await ServeAuthSuccessPage(null, "no_code", "No authorization code was received", ct);
                return;
            }
        }
        
        try
        {
            // Get Keycloak configuration from injected options
            var keycloakConfig = _keycloakOptions.Value;
            if (keycloakConfig == null)
            {
                var configError = "Keycloak configuration not found in dependency injection";
                Logger.LogError("Keycloak configuration is null");
                
                if (isJsonRequest)
                {
                    await SendErrorsAsync(cancellation: ct);
                    AddError(configError);
                    return;
                }
                else
                {
                    await ServeAuthSuccessPage(null, "config_error", configError, ct);
                    return;
                }
            }
            
            Logger.LogInformation("Keycloak config loaded - AuthServerUrl: {AuthServerUrl}, Realm: {Realm}, Resource: {Resource}", 
                keycloakConfig.AuthServerUrl, keycloakConfig.Realm, keycloakConfig.Resource);
            
            // Exchange authorization code for tokens
            var tokenResponse = await ExchangeCodeForTokens(code, keycloakConfig, ct);
            
            // Create response with tokens and user info
            var userInfo = await GetUserInfoFromToken(tokenResponse.AccessToken, keycloakConfig, ct);
            
            var callbackResponse = new AuthCallbackResponse(
                Success: true,
                AuthorizationCode: code,
                State: state,
                AccessToken: tokenResponse.AccessToken,
                RefreshToken: tokenResponse.RefreshToken,
                ExpiresIn: tokenResponse.ExpiresIn,
                TokenType: tokenResponse.TokenType,
                Scope: tokenResponse.Scope,
                UserInfo: userInfo,
                Message: "Token obtained successfully! You can close this window.",
                Timestamp: DateTime.UtcNow
            );
            
            Logger.LogInformation("Successfully exchanged authorization code for tokens for user: {Username}", 
                userInfo?.PreferredUsername ?? "Unknown");
            
            if (isJsonRequest)
            {
                await SendAsync(callbackResponse, cancellation: ct);
            }
            else
            {
                // Serve HTML page with token data
                await ServeAuthSuccessPage(callbackResponse, null, null, ct);
            }
                
        } catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to exchange authorization code for tokens");
            
            if (isJsonRequest)
            {
                await SendErrorsAsync(cancellation: ct);
                AddError($"Token exchange failed: {ex.Message}");
            }
            else
            {
                await ServeAuthSuccessPage(null, "exchange_error", $"Token exchange failed: {ex.Message}", ct);
            }
        }
    }
    
    private async Task ServeAuthSuccessPage(AuthCallbackResponse? tokenData, string? error, string? errorDescription, CancellationToken ct)
    {
        try
        {
            // Read the HTML template
            var htmlPath = Path.Combine(HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>().WebRootPath, "auth-success.html");
            
            string htmlContent;
            if (File.Exists(htmlPath))
            {
                htmlContent = await File.ReadAllTextAsync(htmlPath, ct);
            }
            else
            {
                // Fallback HTML if file doesn't exist
                htmlContent = GenerateFallbackHtml(tokenData, error, errorDescription);
            }
            
            // If we have token data, inject it into the page
            if (tokenData != null)
            {
                var jsonData = JsonSerializer.Serialize(tokenData, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = true
                });
                
                // Inject the token data into the HTML - updated to match the new function name
                htmlContent = htmlContent.Replace("document.addEventListener('DOMContentLoaded', initializePage);", 
                    $"const tokenData = {jsonData}; document.addEventListener('DOMContentLoaded', initializePage);");
            }
            else if (!string.IsNullOrEmpty(error))
            {
                // Inject error data - updated to match the new function name
                var errorData = JsonSerializer.Serialize(new { error, error_description = errorDescription }, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                
                htmlContent = htmlContent.Replace("document.addEventListener('DOMContentLoaded', initializePage);", 
                    $"const errorData = {errorData}; document.addEventListener('DOMContentLoaded', initializePage);");
            }
            
            HttpContext.Response.ContentType = "text/html";
            await HttpContext.Response.WriteAsync(htmlContent, ct);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to serve auth success page");
            
            // Fallback to plain text response
            HttpContext.Response.ContentType = "text/plain";
            if (tokenData != null)
            {
                await HttpContext.Response.WriteAsync($"Success! Access Token: {tokenData.AccessToken}", ct);
            }
            else
            {
                await HttpContext.Response.WriteAsync($"Error: {error} - {errorDescription}", ct);
            }
        }
    }
    
    private static string GenerateFallbackHtml(AuthCallbackResponse? tokenData, string? error, string? errorDescription)
    {
        if (tokenData != null)
        {
            return $@"
<!DOCTYPE html>
<html>
<head><title>Authentication Success</title></head>
<body>
    <h1>🎉 Authentication Successful!</h1>
    <h2>Access Token:</h2>
    <textarea style='width:100%;height:100px;'>{tokenData.AccessToken}</textarea>
    <h2>Refresh Token:</h2>
    <textarea style='width:100%;height:100px;'>{tokenData.RefreshToken}</textarea>
    <p><strong>Expires in:</strong> {tokenData.ExpiresIn} seconds</p>
    <p><strong>User:</strong> {tokenData.UserInfo?.PreferredUsername ?? "Unknown"}</p>
    <p>You can close this window.</p>
</body>
</html>";
        }
        else
        {
            return $@"
<!DOCTYPE html>
<html>
<head><title>Authentication Error</title></head>
<body>
    <h1>❌ Authentication Error</h1>
    <p><strong>Error:</strong> {error}</p>
    <p><strong>Description:</strong> {errorDescription}</p>
</body>
</html>";
        }
    }
    
    private async Task<TokenResponse> ExchangeCodeForTokens(string code, KeycloakConfiguration config, CancellationToken ct)
    {
        Logger.LogInformation("Starting token exchange - AuthServerUrl: {AuthServerUrl}, Realm: {Realm}, Resource: {Resource}", 
            config.AuthServerUrl, config.Realm, config.Resource);
            
        // Validate configuration
        if (string.IsNullOrEmpty(config.AuthServerUrl))
        {
            throw new InvalidOperationException("Keycloak AuthServerUrl is not configured");
        }
        
        if (string.IsNullOrEmpty(config.Realm))
        {
            throw new InvalidOperationException("Keycloak Realm is not configured");
        }
        
        if (string.IsNullOrEmpty(config.Resource))
        {
            throw new InvalidOperationException("Keycloak Resource (ClientId) is not configured");
        }
        
        if (string.IsNullOrEmpty(config.Credentials?.Secret))
        {
            throw new InvalidOperationException("Keycloak Client Secret is not configured");
        }
        
        using var httpClient = new HttpClient();
        
        // Ensure AuthServerUrl doesn't end with slash for consistent URL construction
        var baseUrl = config.AuthServerUrl.TrimEnd('/');
        var tokenUrl = $"{baseUrl}/realms/{config.Realm}/protocol/openid-connect/token";
        var redirectUri = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}/auth/callback";
        
        Logger.LogInformation("Token exchange details - TokenUrl: {TokenUrl}, RedirectUri: {RedirectUri}, Code: {CodeLength} chars", 
            tokenUrl, redirectUri, code?.Length ?? 0);
        
        // Validate the constructed URL
        if (!Uri.TryCreate(tokenUrl, UriKind.Absolute, out var tokenUri))
        {
            throw new InvalidOperationException($"Invalid token URL constructed: {tokenUrl}");
        }
        
        var tokenRequest = new List<KeyValuePair<string, string>>
        {
            new("grant_type", "authorization_code"),
            new("client_id", config.Resource),
            new("client_secret", config.Credentials.Secret),
            new("code", code),
            new("redirect_uri", redirectUri)
        };
        
        var content = new FormUrlEncodedContent(tokenRequest);
        
        try
        {
            var response = await httpClient.PostAsync(tokenUri, content, ct);
            
            Logger.LogInformation("Token exchange response - StatusCode: {StatusCode}", response.StatusCode);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(ct);
                Logger.LogError("Token exchange failed - Status: {StatusCode}, Error: {Error}", 
                    response.StatusCode, errorContent);
                throw new InvalidOperationException($"Token exchange failed: {response.StatusCode} - {errorContent}");
            }
            
            var responseContent = await response.Content.ReadAsStringAsync(ct);
            Logger.LogInformation("Token exchange successful - Response length: {Length} chars", responseContent?.Length ?? 0);
            
            var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(responseContent, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            });
            
            return tokenResponse ?? throw new InvalidOperationException("Failed to deserialize token response");
        }
        catch (HttpRequestException ex)
        {
            Logger.LogError(ex, "HTTP request failed during token exchange");
            throw new InvalidOperationException($"HTTP request failed during token exchange: {ex.Message}", ex);
        }
        catch (TaskCanceledException ex)
        {
            Logger.LogError(ex, "Token exchange request timed out");
            throw new InvalidOperationException("Token exchange request timed out", ex);
        }
    }
    
    private async Task<CallbackUserInfoResponse?> GetUserInfoFromToken(string accessToken, KeycloakConfiguration config, CancellationToken ct)
    {
        try
        {
            Logger.LogInformation("Getting user info from token");
            
            if (string.IsNullOrEmpty(accessToken))
            {
                Logger.LogWarning("Access token is null or empty, skipping user info retrieval");
                return null;
            }
            
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            
            // Ensure AuthServerUrl doesn't end with slash for consistent URL construction
            var baseUrl = config.AuthServerUrl.TrimEnd('/');
            var userInfoUrl = $"{baseUrl}/realms/{config.Realm}/protocol/openid-connect/userinfo";
            
            Logger.LogInformation("User info URL: {UserInfoUrl}", userInfoUrl);
            
            // Validate the constructed URL
            if (!Uri.TryCreate(userInfoUrl, UriKind.Absolute, out var userInfoUri))
            {
                Logger.LogError("Invalid user info URL constructed: {UserInfoUrl}", userInfoUrl);
                return null;
            }
            
            var response = await httpClient.GetAsync(userInfoUri, ct);
            
            Logger.LogInformation("User info response - StatusCode: {StatusCode}", response.StatusCode);
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(ct);
                Logger.LogInformation("User info retrieved successfully - Response length: {Length} chars", content?.Length ?? 0);
                
                return JsonSerializer.Deserialize<CallbackUserInfoResponse>(content, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
                });
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync(ct);
                Logger.LogWarning("Failed to get user info - Status: {StatusCode}, Error: {Error}", 
                    response.StatusCode, errorContent);
                return null;
            }
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to get user info from token: {Error}", ex.Message);
            return null;
        }
    }
}

/// <summary>
/// Response model for the OAuth callback endpoint
/// </summary>
public record AuthCallbackResponse(
    bool Success,
    string? AuthorizationCode,
    string? State,
    string? AccessToken,
    string? RefreshToken,
    int ExpiresIn,
    string? TokenType,
    string? Scope,
    CallbackUserInfoResponse? UserInfo,
    string Message,
    DateTime Timestamp);

/// <summary>
/// Token response from Keycloak
/// </summary>
public record TokenResponse(
    string AccessToken,
    string RefreshToken,
    int ExpiresIn,
    string TokenType,
    string? IdToken,
    string? Scope);

/// <summary>
/// User info response from Keycloak for callback endpoint
/// </summary>
public record CallbackUserInfoResponse(
    string? Sub,
    string? PreferredUsername,
    string? Email,
    string? Name,
    string? GivenName,
    string? FamilyName,
    bool EmailVerified);
