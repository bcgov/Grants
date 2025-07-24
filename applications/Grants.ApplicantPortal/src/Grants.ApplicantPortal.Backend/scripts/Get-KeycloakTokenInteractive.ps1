# Interactive Keycloak Token Retrieval with Browser
# Opens a visible browser, lets you login, then automatically gets the token

param(
    [Parameter(Mandatory=$false)]
    [ValidateSet("IDIR", "BCeID", "BCeID Business")]
    [string]$IdentityProvider = "IDIR",
    
    [Parameter(Mandatory=$false)]
    [string]$ClientId,
    
    [Parameter(Mandatory=$false)]
    [string]$ClientSecret,
    
    [Parameter(Mandatory=$false)]
    [string]$KeycloakUrl,
    
    [Parameter(Mandatory=$false)]
    [string]$Realm,
    
    [Parameter(Mandatory=$false)]
    [string]$RedirectUri = "http://localhost:8080/callback",
    
    [Parameter(Mandatory=$false)]
    [int]$TimeoutSeconds = 300  # 5 minutes for manual login
)

# Import required modules
try {
    Import-Module Selenium -ErrorAction Stop
} catch {
    Write-Host "? Selenium module not found. Installing..." -ForegroundColor Red
    Write-Host "   Running: Install-Module -Name Selenium -Force -Scope CurrentUser" -ForegroundColor Yellow
    
    try {
        Install-Module -Name Selenium -Force -Scope CurrentUser
        Import-Module Selenium
        Write-Host "? Selenium installed successfully!" -ForegroundColor Green
    } catch {
        Write-Host "? Failed to install Selenium. Please run manually:" -ForegroundColor Red
        Write-Host "   Install-Module -Name Selenium -Force -Scope CurrentUser" -ForegroundColor Yellow
        exit 1
    }
}

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

function Get-ConfigurationInteractively {
    Write-Host ""
    Write-Host "?? Keycloak Configuration Setup" -ForegroundColor Yellow
    Write-Host "=================================" -ForegroundColor Yellow
    Write-Host ""
    
    # Try to load from user secrets first
    $webProjectPath = "$PSScriptRoot\..\src\Grants.ApplicantPortal.API.Web"
    $config = @{}
    
    if (Test-Path $webProjectPath) {
        try {
            Write-Info "Checking for existing user secrets..."
            Push-Location $webProjectPath
            $secretsJson = dotnet user-secrets list --json 2>$null
            
            if ($secretsJson -and $secretsJson -ne "No secrets configured for this application.") {
                $secrets = $secretsJson | ConvertFrom-Json
                
                if ($secrets.'Keycloak:Resource' -and $secrets.'Keycloak:Credentials:Secret' -and 
                    $secrets.'Keycloak:AuthServerUrl' -and $secrets.'Keycloak:Realm') {
                    
                    Write-Success "Found existing configuration in user secrets!"
                    $config = @{
                        ClientId = $secrets.'Keycloak:Resource'
                        ClientSecret = $secrets.'Keycloak:Credentials:Secret'  
                        KeycloakUrl = $secrets.'Keycloak:AuthServerUrl'
                        Realm = $secrets.'Keycloak:Realm'
                    }
                    
                    Write-Host "  Client ID: $($config.ClientId)" -ForegroundColor White
                    Write-Host "  Keycloak URL: $($config.KeycloakUrl)" -ForegroundColor White
                    Write-Host "  Realm: $($config.Realm)" -ForegroundColor White
                    Write-Host "  Client Secret: [LOADED]" -ForegroundColor White
                    Write-Host ""
                    
                    $useExisting = Read-Host "Use this configuration? (Y/n)"
                    if ($useExisting -eq "" -or $useExisting.ToLower() -eq "y") {
                        return $config
                    }
                }
            }
        } catch {
            Write-Warning "Could not load from user secrets: $($_.Exception.Message)"
        } finally {
            Pop-Location
        }
    }
    
    # Get configuration interactively
    Write-Host "Please provide your Keycloak configuration:" -ForegroundColor Cyan
    Write-Host ""
    
    do {
        $config.ClientId = Read-Host "Client ID (Resource)"
    } while ([string]::IsNullOrWhiteSpace($config.ClientId))
    
    do {
        $clientSecretSecure = Read-Host "Client Secret" -AsSecureString
        $config.ClientSecret = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto([System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($clientSecretSecure))
    } while ([string]::IsNullOrWhiteSpace($config.ClientSecret))
    
    do {
        $config.KeycloakUrl = Read-Host "Keycloak URL (e.g., https://dev.loginproxy.gov.bc.ca/auth)"
    } while ([string]::IsNullOrWhiteSpace($config.KeycloakUrl))
    
    do {
        $config.Realm = Read-Host "Realm (e.g., standard)"
    } while ([string]::IsNullOrWhiteSpace($config.Realm))
    
    # Ask if they want to save to user secrets
    Write-Host ""
    $saveToSecrets = Read-Host "Save this configuration to user secrets for future use? (Y/n)"
    if ($saveToSecrets -eq "" -or $saveToSecrets.ToLower() -eq "y") {
        if (Test-Path $webProjectPath) {
            try {
                Push-Location $webProjectPath
                
                Write-Info "Saving configuration to user secrets..."
                dotnet user-secrets init 2>$null
                dotnet user-secrets set "Keycloak:Resource" $config.ClientId
                dotnet user-secrets set "Keycloak:Credentials:Secret" $config.ClientSecret
                dotnet user-secrets set "Keycloak:AuthServerUrl" $config.KeycloakUrl
                dotnet user-secrets set "Keycloak:Realm" $config.Realm
                
                Write-Success "Configuration saved to user secrets!"
            } catch {
                Write-Warning "Could not save to user secrets: $($_.Exception.Message)"
            } finally {
                Pop-Location
            }
        }
    }
    
    return $config
}

function Get-KeycloakTokenInteractive {
    param(
        [string]$IdentityProvider,
        [string]$ClientId,
        [string]$ClientSecret,
        [string]$KeycloakUrl,
        [string]$Realm,
        [string]$RedirectUri,
        [int]$TimeoutSeconds
    )
    
    Write-Status "Starting interactive Keycloak authentication..."
    Write-Info "Browser will open for you to login with $IdentityProvider"
    Write-Info "After successful login, the browser will close automatically"
    
    # Setup Chrome options for visible browser
    $chromeOptions = New-Object OpenQA.Selenium.Chrome.ChromeOptions
    $chromeOptions.AddArgument("--disable-gpu")
    $chromeOptions.AddArgument("--no-sandbox")
    $chromeOptions.AddArgument("--disable-dev-shm-usage")
    $chromeOptions.AddArgument("--window-size=1200,800")
    $chromeOptions.AddArgument("--start-maximized")
    
    $driver = $null
    
    try {
        # Initialize Chrome driver
        Write-Status "Opening browser..."
        $driver = New-Object OpenQA.Selenium.Chrome.ChromeDriver($chromeOptions)
        $driver.Manage().Timeouts().ImplicitWait = [TimeSpan]::FromSeconds(10)
        
        # Step 1: Navigate to authorization URL
        $state = [System.Guid]::NewGuid().ToString()
        $authUrl = "$KeycloakUrl/realms/$Realm/protocol/openid-connect/auth?" +
                   "client_id=$ClientId&" +
                   "response_type=code&" +
                   "scope=openid profile email&" +
                   "redirect_uri=$([System.Web.HttpUtility]::UrlEncode($RedirectUri))&" +
                   "state=$state"
        
        Write-Status "Navigating to Keycloak login page..."
        $driver.Navigate().GoToUrl($authUrl)
        
        Write-Host ""
        Write-Host "?? Browser opened!" -ForegroundColor Green
        Write-Host "   Please complete your $IdentityProvider login in the browser window" -ForegroundColor Yellow
        Write-Host "   The script will automatically continue once you're authenticated" -ForegroundColor Yellow
        Write-Host ""
        
        # Wait for redirect to callback URL
        Write-Status "Waiting for authentication to complete..."
        
        $maxWaitTime = $TimeoutSeconds
        $waitTime = 0
        $authCode = $null
        $lastUrl = ""
        
        while ($waitTime -lt $maxWaitTime) {
            $currentUrl = $driver.Url
            
            # Show URL changes for transparency
            if ($currentUrl -ne $lastUrl) {
                $urlHost = ([System.Uri]$currentUrl).Host
                Write-Info "Current page: $urlHost"
                $lastUrl = $currentUrl
            }
            
            # Check if we've been redirected to our callback URL
            if ($currentUrl.StartsWith($RedirectUri)) {
                # Extract code from URL
                $uri = [System.Uri]$currentUrl
                $query = [System.Web.HttpUtility]::ParseQueryString($uri.Query)
                $authCode = $query["code"]
                
                if ($authCode) {
                    Write-Success "Authentication completed successfully!"
                    break
                }
            }
            
            # Check for common error indicators
            $currentTitle = $driver.Title
            if ($currentTitle -like "*error*" -or $currentTitle -like "*Error*" -or $currentUrl -like "*error*") {
                Write-Host "??  Potential error detected on page: $currentTitle" -ForegroundColor Yellow
                Write-Info "Please check the browser for any error messages"
            }
            
            Start-Sleep -Seconds 2
            $waitTime += 2
            
            # Show progress every 30 seconds
            if ($waitTime % 30 -eq 0) {
                $remainingTime = $maxWaitTime - $waitTime
                Write-Info "Still waiting for authentication... ($remainingTime seconds remaining)"
            }
        }
        
        if (-not $authCode) {
            throw "Authentication did not complete within $TimeoutSeconds seconds. Please try again."
        }
        
        Write-Status "Closing browser..."
        $driver.Quit()
        $driver = $null
        
        # Step 2: Exchange authorization code for tokens
        Write-Status "Exchanging authorization code for JWT token..."
        
        $tokenUrl = "$KeycloakUrl/realms/$Realm/protocol/openid-connect/token"
        $tokenBody = @{
            grant_type = "authorization_code"
            client_id = $ClientId
            client_secret = $ClientSecret
            code = $authCode
            redirect_uri = $RedirectUri
        }
        
        $tokenResponse = Invoke-RestMethod -Uri $tokenUrl -Method Post -Body $tokenBody -ContentType "application/x-www-form-urlencoded"
        
        Write-Success "JWT token obtained successfully!"
        
        return @{
            AccessToken = $tokenResponse.access_token
            RefreshToken = $tokenResponse.refresh_token
            ExpiresIn = $tokenResponse.expires_in
            TokenType = $tokenResponse.token_type
            IdToken = $tokenResponse.id_token
            Scope = $tokenResponse.scope
        }
        
    } catch {
        Write-Host "? Error during authentication: $($_.Exception.Message)" -ForegroundColor Red
        throw
    } finally {
        if ($driver) {
            Write-Status "Ensuring browser is closed..."
            try {
                $driver.Quit()
            } catch {
                # Browser may already be closed
            }
        }
    }
}

# Main execution
try {
    Write-Host ""
    Write-Host "?? Interactive Keycloak Token Generator" -ForegroundColor Cyan
    Write-Host "=======================================" -ForegroundColor Cyan
    Write-Host ""
    
    # Get configuration (interactively or from parameters)
    if (-not $ClientId -or -not $ClientSecret -or -not $KeycloakUrl -or -not $Realm) {
        $config = Get-ConfigurationInteractively
        
        # Use provided parameters or fall back to interactive config
        if (-not $ClientId) { $ClientId = $config.ClientId }
        if (-not $ClientSecret) { $ClientSecret = $config.ClientSecret }
        if (-not $KeycloakUrl) { $KeycloakUrl = $config.KeycloakUrl }
        if (-not $Realm) { $Realm = $config.Realm }
    }
    
    Write-Host "Configuration Summary:" -ForegroundColor Yellow
    Write-Host "  Identity Provider: $IdentityProvider" -ForegroundColor White
    Write-Host "  Client ID: $ClientId" -ForegroundColor White
    Write-Host "  Keycloak URL: $KeycloakUrl" -ForegroundColor White
    Write-Host "  Realm: $Realm" -ForegroundColor White
    Write-Host "  Redirect URI: $RedirectUri" -ForegroundColor White
    Write-Host ""
    
    $proceed = Read-Host "Proceed with token generation? (Y/n)"
    if ($proceed -ne "" -and $proceed.ToLower() -ne "y") {
        Write-Host "Token generation cancelled." -ForegroundColor Yellow
        exit 0
    }
    
    $tokenResult = Get-KeycloakTokenInteractive -IdentityProvider $IdentityProvider -ClientId $ClientId -ClientSecret $ClientSecret -KeycloakUrl $KeycloakUrl -Realm $Realm -RedirectUri $RedirectUri -TimeoutSeconds $TimeoutSeconds
    
    Write-Host ""
    Write-Host "?? Token Generation Completed!" -ForegroundColor Green
    Write-Host "==============================" -ForegroundColor Green
    Write-Host ""
    Write-Host "Token Details:" -ForegroundColor Yellow
    Write-Host "- Type: $($tokenResult.TokenType)" -ForegroundColor White
    Write-Host "- Expires In: $($tokenResult.ExpiresIn) seconds ($([math]::Round($tokenResult.ExpiresIn / 60, 1)) minutes)" -ForegroundColor White
    Write-Host "- Scope: $($tokenResult.Scope)" -ForegroundColor White
    Write-Host ""
    
    # Save token to file for easy access
    $tokenFile = "keycloak-token.json"
    $tokenResult | ConvertTo-Json -Depth 10 | Out-File -FilePath $tokenFile -Encoding UTF8
    Write-Success "Token saved to: $tokenFile"
    
    # Set environment variable for easy access
    $env:KEYCLOAK_TOKEN = $tokenResult.AccessToken
    Write-Success "Token set in environment variable: KEYCLOAK_TOKEN"
    
    Write-Host ""
    Write-Host "?? Test Your API:" -ForegroundColor Cyan
    Write-Host "=================" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "PowerShell:" -ForegroundColor Yellow
    Write-Host '  $headers = @{ "Authorization" = "Bearer $env:KEYCLOAK_TOKEN" }' -ForegroundColor Gray
    Write-Host '  Invoke-RestMethod -Uri "https://localhost:7000/Auth/userinfo" -Headers $headers' -ForegroundColor Gray
    Write-Host ""
    Write-Host "curl:" -ForegroundColor Yellow
    Write-Host '  curl -H "Authorization: Bearer $env:KEYCLOAK_TOKEN" https://localhost:7000/Auth/userinfo' -ForegroundColor Gray
    Write-Host ""
    
    # Show a snippet of the token for verification
    $tokenSnippet = $tokenResult.AccessToken.Substring(0, [Math]::Min(50, $tokenResult.AccessToken.Length)) + "..."
    Write-Host "Access Token (first 50 chars): $tokenSnippet" -ForegroundColor White
    Write-Host ""
    
    Write-Success "Ready to use! Your token is valid for $([math]::Round($tokenResult.ExpiresIn / 60, 1)) minutes."
    
} catch {
    Write-Host ""
    Write-Host "? Script failed: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host ""
    Write-Host "?? Troubleshooting tips:" -ForegroundColor Yellow
    Write-Host "  1. Make sure Chrome browser is installed" -ForegroundColor White
    Write-Host "  2. Check your network connection to Keycloak" -ForegroundColor White
    Write-Host "  3. Verify your Keycloak configuration is correct" -ForegroundColor White
    Write-Host "  4. Try running the script again" -ForegroundColor White
    Write-Host ""
    exit 1
}