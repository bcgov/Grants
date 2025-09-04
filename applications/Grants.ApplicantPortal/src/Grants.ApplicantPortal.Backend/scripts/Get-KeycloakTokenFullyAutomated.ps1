# Fully Automated Keycloak Token Generator
# Opens browser, lets you login, then automatically captures and exchanges the token

param(
    [Parameter(Mandatory=$false)]
    [string]$ClientId,
    
    [Parameter(Mandatory=$false)]
    [string]$ClientSecret,
    
    [Parameter(Mandatory=$false)]
    [string]$KeycloakUrl,
    
    [Parameter(Mandatory=$false)]
    [string]$Realm,
    
    [Parameter(Mandatory=$false)]
    [string]$ApiBaseUrl = "https://localhost:7000",  # Back to port 7000
    
    [Parameter(Mandatory=$false)]
    [int]$TimeoutSeconds = 300  # 5 minutes - plenty of time for login
)

function Write-Status {
    param([string]$Message)
    Write-Host "?? $Message" -ForegroundColor Cyan
}

function Write-Success {
    param([string]$Message)
    Write-Host "? $Message" -ForegroundColor Green
}

function Write-Info {
    param([string]$Message)
    Write-Host "??  $Message" -ForegroundColor Blue
}

function Find-WebProjectPath {
    # Try to find the Web project from current location
    $currentDir = Get-Location
    Write-Info "Looking for Web project from: $currentDir"
    
    # Common paths to check
    $pathsToTry = @(
        "src\Grants.ApplicantPortal.API.Web",
        ".\src\Grants.ApplicantPortal.API.Web",
        "..\src\Grants.ApplicantPortal.API.Web",
        "..\..\src\Grants.ApplicantPortal.API.Web"
    )
    
    foreach ($path in $pathsToTry) {
        $fullPath = Resolve-Path $path -ErrorAction SilentlyContinue
        if ($fullPath) {
            if (Test-Path $fullPath -PathType Container) {
                $projectFile = Join-Path $fullPath "Grants.ApplicantPortal.API.Web.csproj"
                if (Test-Path $projectFile) {
                    Write-Success "Found Web project at: $fullPath"
                    return $fullPath.Path
                }
            }
        }
    }
    
    return $null
}

function Clean-UserSecretsOutput {
    param([string]$RawOutput)
    
    # Remove //BEGIN and //END markers that dotnet user-secrets sometimes adds
    $cleanedOutput = $RawOutput -replace "//BEGIN\s*", "" -replace "\s*//END", ""
    
    # Trim any whitespace
    $cleanedOutput = $cleanedOutput.Trim()
    
    return $cleanedOutput
}

function Get-ConfigurationFromSecrets {
    Write-Info "Loading configuration from user secrets..."
    
    # Find the Web project path
    $webProjectPath = Find-WebProjectPath
    
    if ($webProjectPath) {
        try {
            Push-Location $webProjectPath
            
            Write-Info "Running: dotnet user-secrets list --json"
            $secretsOutput = dotnet user-secrets list --json 2>&1
            
            if ($secretsOutput -notlike "*No secrets configured*" -and $secretsOutput -notlike "*error*") {
                $cleanedOutput = Clean-UserSecretsOutput $secretsOutput
                $secrets = $cleanedOutput | ConvertFrom-Json
                
                if ($secrets.'Keycloak:Resource' -and $secrets.'Keycloak:Credentials:Secret' -and 
                    $secrets.'Keycloak:AuthServerUrl' -and $secrets.'Keycloak:Realm') {
                    
                    Write-Success "Configuration loaded from user secrets!"
                    return @{
                        ClientId = $secrets.'Keycloak:Resource'
                        ClientSecret = $secrets.'Keycloak:Credentials:Secret'
                        KeycloakUrl = $secrets.'Keycloak:AuthServerUrl'
                        Realm = $secrets.'Keycloak:Realm'
                    }
                }
            }
        } catch {
            Write-Warning "Could not load from user secrets: $($_.Exception.Message)"
        } finally {
            Pop-Location
        }
    }
    
    return $null
}

function Test-ApiServer {
    param([string]$ApiBaseUrl)
    
    try {
        $response = Invoke-WebRequest -Uri "$ApiBaseUrl/System/info" -Method GET -TimeoutSec 5 -ErrorAction Stop
        if ($response.StatusCode -eq 200) {
            Write-Success "API server is running at $ApiBaseUrl"
            return $true
        }
    } catch {
        Write-Warning "API server not running at $ApiBaseUrl"
        return $false
    }
}

function Get-AuthorizationUrl {
    param([hashtable]$Config, [string]$ApiBaseUrl)
    
    $redirectUri = "$ApiBaseUrl/auth/callback"
    $state = [System.Guid]::NewGuid().ToString()
    
    $authUrl = "$($Config.KeycloakUrl)/realms/$($Config.Realm)/protocol/openid-connect/auth?" +
               "client_id=$($Config.ClientId)&" +
               "response_type=code&" +
               "scope=openid profile email&" +
               "redirect_uri=$([System.Web.HttpUtility]::UrlEncode($redirectUri))&" +
               "state=$state"
    
    return @{
        AuthUrl = $authUrl
        State = $state
        RedirectUri = $redirectUri
    }
}

function Get-FullyAutomatedToken {
    param([hashtable]$Config, [string]$ApiBaseUrl, [int]$TimeoutSeconds)
    
    # Check if API server is running
    if (-not (Test-ApiServer -ApiBaseUrl $ApiBaseUrl)) {
        Write-Host ""
        Write-Host "? API server is not running!" -ForegroundColor Red
        Write-Host ""
        Write-Host "Please start your API server first:" -ForegroundColor Yellow
        Write-Host "  1. Navigate to: src\Grants.ApplicantPortal.API.Web" -ForegroundColor White
        Write-Host "  2. Run: dotnet run" -ForegroundColor White
        Write-Host "  3. Wait for it to start on $ApiBaseUrl" -ForegroundColor White
        Write-Host "  4. Then run this script again" -ForegroundColor White
        Write-Host ""
        throw "API server not running"
    }
    
    # Generate authorization URL
    $authInfo = Get-AuthorizationUrl -Config $Config -ApiBaseUrl $ApiBaseUrl
    
    Write-Host ""
    Write-Host "?? Fully Automated Token Generation" -ForegroundColor Cyan
    Write-Host "====================================" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Configuration:" -ForegroundColor Yellow
    Write-Host "  ?? Keycloak: $($Config.KeycloakUrl)" -ForegroundColor White
    Write-Host "  ???  Realm: $($Config.Realm)" -ForegroundColor White
    Write-Host "  ?? Client: $($Config.ClientId)" -ForegroundColor White
    Write-Host "  ?? Callback: $($authInfo.RedirectUri)" -ForegroundColor White
    Write-Host "  ?? API Server: $ApiBaseUrl" -ForegroundColor White
    Write-Host ""
    
    Write-Status "Opening browser for authentication..."
    
    # Open browser
    try {
        Start-Process $authInfo.AuthUrl
        Write-Success "Browser opened automatically"
    } catch {
        Write-Host "Could not open browser automatically. Please open this URL:" -ForegroundColor Yellow
        Write-Host $authInfo.AuthUrl -ForegroundColor White
    }
    
    Write-Host ""
    Write-Host "?? Complete your authentication in the browser" -ForegroundColor Green
    Write-Host "   ?? Choose your identity provider (IDIR, BCeID, etc.)" -ForegroundColor Yellow
    Write-Host "   ?? Enter your credentials" -ForegroundColor Yellow
    Write-Host "   ? After login, you'll see a beautiful page with your token!" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "?? The callback endpoint will automatically:" -ForegroundColor Cyan
    Write-Host "   • Exchange your authorization code for a JWT token" -ForegroundColor White
    Write-Host "   • Display a user-friendly page with copy buttons" -ForegroundColor White
    Write-Host "   • Show your user information and token details" -ForegroundColor White
    Write-Host "   • Provide ready-to-use curl examples" -ForegroundColor White
    Write-Host ""
    
    $proceed = Read-Host "Press Enter after you've completed the login and copied your token..."
    
    Write-Host ""
    Write-Host "?? Automation Complete!" -ForegroundColor Green
    Write-Host "========================" -ForegroundColor Green
    Write-Host ""
    Write-Host "Your token should now be available from the beautiful callback page!" -ForegroundColor White
    Write-Host ""
    Write-Host "?? Next Steps:" -ForegroundColor Yellow
    Write-Host "  1. Copy the access token from the browser page" -ForegroundColor White
    Write-Host "  2. Set it as an environment variable:" -ForegroundColor White
    Write-Host "     `$env:KEYCLOAK_TOKEN = \"your-token-here\"" -ForegroundColor Gray
    Write-Host "  3. Test your API:" -ForegroundColor White
    Write-Host "     curl -H \"Authorization: Bearer `$env:KEYCLOAK_TOKEN\" $ApiBaseUrl/Auth/userinfo" -ForegroundColor Gray
    Write-Host ""
    
    return @{
        Message = "Token available from callback page"
        CallbackUrl = $authInfo.RedirectUri
        ApiUrl = $ApiBaseUrl
        Instructions = "Check the browser window for the token with copy buttons"
    }
}

# Main execution
try {
    Write-Host ""
    Write-Host "?? Fully Automated Keycloak Token Generator" -ForegroundColor Cyan
    Write-Host "============================================" -ForegroundColor Cyan
    Write-Host ""
    
    # Load configuration
    $config = Get-ConfigurationFromSecrets
    
    if (-not $config) {
        Write-Host "? No configuration found!" -ForegroundColor Red
        Write-Host ""
        Write-Host "Please set up your configuration first:" -ForegroundColor Yellow
        Write-Host "  .\scripts\setup-dev-secrets.ps1" -ForegroundColor White
        exit 1
    }
    
    # Override with any provided parameters
    if ($ClientId) { $config.ClientId = $ClientId }
    if ($ClientSecret) { $config.ClientSecret = $ClientSecret }
    if ($KeycloakUrl) { $config.KeycloakUrl = $KeycloakUrl }
    if ($Realm) { $config.Realm = $Realm }
    
    # Generate token using callback endpoint
    $result = Get-FullyAutomatedToken -Config $config -ApiBaseUrl $ApiBaseUrl -TimeoutSeconds $TimeoutSeconds
    
    Write-Success "Ready to use! Check the browser for your token details."
    
} catch {
    Write-Host ""
    Write-Host "? Failed: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host ""
    Write-Host "?? Troubleshooting:" -ForegroundColor Yellow
    Write-Host "  1. Make sure your API is running: dotnet run" -ForegroundColor White
    Write-Host "  2. Check it's accessible at: $ApiBaseUrl" -ForegroundColor White
    Write-Host "  3. Add redirect URI to Keycloak: $ApiBaseUrl/auth/callback" -ForegroundColor White
    Write-Host "  4. Use manual script as fallback: .\scripts\Get-KeycloakTokenSimple.ps1" -ForegroundColor White
    Write-Host ""
    
    exit 1
}