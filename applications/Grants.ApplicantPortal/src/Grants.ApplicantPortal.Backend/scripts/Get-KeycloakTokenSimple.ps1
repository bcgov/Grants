# Simple Interactive Keycloak Token Generator with Browser Detection
# Opens browser, lets you login naturally, then automatically gets your token

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
    [string]$RedirectUri = "https://localhost:7000/auth/callback",  # Back to port 7000
    
    [Parameter(Mandatory=$false)]
    [int]$TimeoutSeconds = 300  # 5 minutes - plenty of time for login
)

# Import required modules
try {
    Import-Module Selenium -ErrorAction Stop
} catch {
    Write-Host "? Selenium module not found. Installing..." -ForegroundColor Red
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

function Find-WebProjectPath {
    # Try to find the Web project from current location
    $currentDir = Get-Location
    Write-Info "Looking for Web project from: $currentDir"
    
    # Common paths to check (relative to script location and current directory)
    $pathsToTry = @(
        "src\Grants.ApplicantPortal.API.Web",                           # From repository root
        ".\src\Grants.ApplicantPortal.API.Web",                         # From current directory
        "..\src\Grants.ApplicantPortal.API.Web",                        # From scripts directory
        "..\..\src\Grants.ApplicantPortal.API.Web",                     # From deeper nesting
        "Grants.ApplicantPortal.API.Web",                              # Direct folder
        "src\Grants.ApplicantPortal.API.Web\Grants.ApplicantPortal.API.Web.csproj"  # Direct to project file
    )
    
    foreach ($path in $pathsToTry) {
        $fullPath = Resolve-Path $path -ErrorAction SilentlyContinue
        if ($fullPath) {
            # Check if it's a directory with the project file
            if (Test-Path $fullPath -PathType Container) {
                $projectFile = Join-Path $fullPath "Grants.ApplicantPortal.API.Web.csproj"
                if (Test-Path $projectFile) {
                    Write-Success "Found Web project at: $fullPath"
                    return $fullPath.Path
                }
            }
            # Check if it's the project file directly
            elseif ($path.EndsWith(".csproj") -and (Test-Path $fullPath)) {
                $projectDir = Split-Path $fullPath.Path -Parent
                Write-Success "Found Web project at: $projectDir"
                return $projectDir
            }
        }
    }
    
    # If not found, search more broadly
    Write-Info "Searching for Web project in current directory tree..."
    $searchResult = Get-ChildItem -Recurse -Name "Grants.ApplicantPortal.API.Web.csproj" -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($searchResult) {
        $projectDir = Split-Path (Resolve-Path $searchResult).Path -Parent
        Write-Success "Found Web project at: $projectDir"
        return $projectDir
    }
    
    return $null
}

function Test-BrowserAvailability {
    Write-Info "Checking available browsers..."
    
    $browsers = @()
    
    # Check for Chrome
    $chromePaths = @(
        "${env:ProgramFiles}\Google\Chrome\Application\chrome.exe",
        "${env:ProgramFiles(x86)}\Google\Chrome\Application\chrome.exe",
        "${env:LOCALAPPDATA}\Google\Chrome\Application\chrome.exe"
    )
    
    foreach ($path in $chromePaths) {
        if (Test-Path $path) {
            $browsers += @{ Name = "Chrome"; Path = $path; Available = $true }
            Write-Success "Found Chrome at: $path"
            break
        }
    }
    
    if ($browsers.Count -eq 0 -or -not ($browsers | Where-Object { $_.Name -eq "Chrome" })) {
        $browsers += @{ Name = "Chrome"; Path = ""; Available = $false }
        Write-Info "Chrome not found"
    }
    
    # Check for Edge
    $edgePaths = @(
        "${env:ProgramFiles(x86)}\Microsoft\Edge\Application\msedge.exe",
        "${env:ProgramFiles}\Microsoft\Edge\Application\msedge.exe"
    )
    
    foreach ($path in $edgePaths) {
        if (Test-Path $path) {
            $browsers += @{ Name = "Edge"; Path = $path; Available = $true }
            Write-Success "Found Edge at: $path"
            break
        }
    }
    
    if (-not ($browsers | Where-Object { $_.Name -eq "Edge" })) {
        $browsers += @{ Name = "Edge"; Path = ""; Available = $false }
        Write-Info "Edge not found"
    }
    
    return $browsers
}

function Clean-UserSecretsOutput {
    param([string]$RawOutput)
    
    # Remove //BEGIN and //END markers that dotnet user-secrets sometimes adds
    $cleanedOutput = $RawOutput -replace "//BEGIN\s*", "" -replace "\s*//END", ""
    
    # Trim any whitespace
    $cleanedOutput = $cleanedOutput.Trim()
    
    return $cleanedOutput
}

function Get-ConfigurationInteractively {
    Write-Host ""
    Write-Host "?? Keycloak Configuration" -ForegroundColor Yellow
    Write-Host "=========================" -ForegroundColor Yellow
    Write-Host ""
    
    # Find the Web project path
    $webProjectPath = Find-WebProjectPath
    $config = @{}
    
    if ($webProjectPath) {
        try {
            Write-Info "Checking for existing user secrets..."
            Push-Location $webProjectPath
            
            # First check if user secrets are initialized
            Write-Info "Running: dotnet user-secrets list --json"
            $secretsOutput = dotnet user-secrets list --json 2>&1
            
            # Check for common error messages
            if ($secretsOutput -like "*No secrets configured*") {
                Write-Info "No user secrets configured yet"
            }
            elseif ($secretsOutput -like "*error*" -and $secretsOutput -notlike "*//BEGIN*") {
                Write-Warning "User secrets command failed: $secretsOutput"
                Write-Info "This might be due to uninitialized secrets or path issues"
            }
            else {
                # Clean the output and try to parse as JSON
                try {
                    $cleanedOutput = Clean-UserSecretsOutput $secretsOutput
                    Write-Info "Cleaned secrets output, attempting to parse JSON..."
                    
                    $secrets = $cleanedOutput | ConvertFrom-Json
                    
                    # Build configuration from flat structure (dotnet user-secrets uses flat keys)
                    if ($secrets.'Keycloak:Resource' -and $secrets.'Keycloak:Credentials:Secret' -and 
                        $secrets.'Keycloak:AuthServerUrl' -and $secrets.'Keycloak:Realm') {
                        
                        Write-Success "Found existing configuration!"
                        $config = @{
                            ClientId = $secrets.'Keycloak:Resource'
                            ClientSecret = $secrets.'Keycloak:Credentials:Secret'
                            KeycloakUrl = $secrets.'Keycloak:AuthServerUrl'
                            Realm = $secrets.'Keycloak:Realm'
                        }
                        
                        Write-Host "  Client ID (Resource): $($config.ClientId)" -ForegroundColor White
                        Write-Host "  Keycloak URL: $($config.KeycloakUrl)" -ForegroundColor White
                        Write-Host "  Realm: $($config.Realm)" -ForegroundColor White
                        Write-Host "  Client Secret: [LOADED]" -ForegroundColor White
                        Write-Host ""
                        
                        return $config
                    } else {
                        Write-Warning "Keycloak configuration found but incomplete"
                        $availableKeys = $secrets.PSObject.Properties.Name | Where-Object { $_ -like "Keycloak:*" }
                        Write-Info "Available Keycloak keys: $($availableKeys -join ', ')"
                    }
                }
                catch {
                    Write-Warning "Could not parse user secrets output as JSON: $($_.Exception.Message)"
                    Write-Info "Raw output was: $secretsOutput"
                    Write-Info "Cleaned output was: $cleanedOutput"
                }
            }
        } catch {
            Write-Warning "Could not load from user secrets: $($_.Exception.Message)"
        } finally {
            Pop-Location
        }
    } else {
        Write-Warning "Could not find Web project directory"
        Write-Info "Please ensure you're running from the repository root or provide configuration manually"
    }
    
    # Get configuration interactively
    Write-Host "Please provide your Keycloak configuration:" -ForegroundColor Cyan
    Write-Host ""
    
    do {
        $config.ClientId = Read-Host "Client ID (also called Resource in Keycloak)"
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
    
    return $config
}

function Get-ValidRedirectUris {
    Write-Host ""
    Write-Host "?? Redirect URI Options" -ForegroundColor Yellow
    Write-Host "======================" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Choose a redirect URI to use:" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "1. https://localhost:7000/auth/callback (your app's callback)" -ForegroundColor White
    Write-Host "2. http://localhost:8080/callback (generic callback)" -ForegroundColor White
    Write-Host "3. http://127.0.0.1:8080/callback (alternative localhost)" -ForegroundColor White
    Write-Host "4. Custom redirect URI" -ForegroundColor White
    Write-Host ""
    
    $choice = Read-Host "Select option (1-4)"
    
    switch ($choice) {
        "1" { 
            Write-Info "Using: https://localhost:7000/auth/callback"
            return "https://localhost:7000/auth/callback" 
        }
        "2" { 
            Write-Info "Using: http://localhost:8080/callback"
            return "http://localhost:8080/callback" 
        }
        "3" { 
            Write-Info "Using: http://127.0.0.1:8080/callback"
            return "http://127.0.0.1:8080/callback" 
        }
        "4" { 
            $customUri = Read-Host "Enter custom redirect URI"
            Write-Info "Using: $customUri"
            return $customUri
        }
        default { 
            Write-Info "Invalid choice, using default: https://localhost:7000/auth/callback"
            return "https://localhost:7000/auth/callback"
        }
    }
}

function Start-BrowserAuthentication {
    param(
        [string]$ClientId,
        [string]$ClientSecret,
        [string]$KeycloakUrl,
        [string]$Realm,
        [string]$RedirectUri,
        [int]$TimeoutSeconds
    )
    
    # Check available browsers
    $browsers = Test-BrowserAvailability
    $availableBrowsers = $browsers | Where-Object { $_.Available -eq $true }
    
    if ($availableBrowsers.Count -eq 0) {
        Write-Host "? No supported browsers found!" -ForegroundColor Red
        Write-Host ""
        Write-Host "Please install one of the following:" -ForegroundColor Yellow
        Write-Host "  - Google Chrome: https://www.google.com/chrome/" -ForegroundColor White
        Write-Host "  - Microsoft Edge: https://www.microsoft.com/edge" -ForegroundColor White
        Write-Host ""
        
        # Offer manual fallback
        $useManual = Read-Host "Would you like to use manual authentication instead? (Y/n)"
        if ($useManual -eq "" -or $useManual.ToLower() -eq "y") {
            return Get-TokenManually -ClientId $ClientId -ClientSecret $ClientSecret -KeycloakUrl $KeycloakUrl -Realm $Realm -RedirectUri $RedirectUri
        } else {
            throw "No browsers available for automation"
        }
    }
    
    # Try browsers in preference order
    $browserPriority = @("Chrome", "Edge")
    $selectedBrowser = $null
    
    foreach ($browserName in $browserPriority) {
        $browser = $availableBrowsers | Where-Object { $_.Name -eq $browserName }
        if ($browser) {
            $selectedBrowser = $browser
            break
        }
    }
    
    if (-not $selectedBrowser) {
        $selectedBrowser = $availableBrowsers[0]
    }
    
    Write-Success "Using $($selectedBrowser.Name) browser for authentication"
    
    # Setup browser-specific options
    $driver = $null
    try {
        switch ($selectedBrowser.Name) {
            "Chrome" {
                $chromeOptions = New-Object OpenQA.Selenium.Chrome.ChromeOptions
                $chromeOptions.AddArgument("--disable-web-security")
                $chromeOptions.AddArgument("--disable-features=VizDisplayCompositor")
                $chromeOptions.AddArgument("--window-size=1200,800")
                
                Write-Status "Starting Chrome browser..."
                $driver = New-Object OpenQA.Selenium.Chrome.ChromeDriver($chromeOptions)
            }
            "Edge" {
                $edgeOptions = New-Object OpenQA.Selenium.Edge.EdgeOptions
                $edgeOptions.AddArgument("--disable-web-security")
                $edgeOptions.AddArgument("--window-size=1200,800")
                
                Write-Status "Starting Edge browser..."
                $driver = New-Object OpenQA.Selenium.Edge.EdgeDriver($edgeOptions)
            }
        }
        
        $driver.Manage().Timeouts().ImplicitWait = [TimeSpan]::FromSeconds(10)
        
        # Navigate to Keycloak authorization URL
        $state = [System.Guid]::NewGuid().ToString()
        $authUrl = "$KeycloakUrl/realms/$Realm/protocol/openid-connect/auth?" +
                   "client_id=$ClientId&" +
                   "response_type=code&" +
                   "scope=openid profile email&" +
                   "redirect_uri=$([System.Web.HttpUtility]::UrlEncode($RedirectUri))&" +
                   "state=$state"
        
        Write-Status "Navigating to Keycloak..."
        Write-Info "Using redirect URI: $RedirectUri"
        $driver.Navigate().GoToUrl($authUrl)
        
        Write-Host ""
        Write-Host "?? Browser is open!" -ForegroundColor Green
        Write-Host "   ?? Complete your authentication in the browser" -ForegroundColor Yellow
        Write-Host "   ?? Keycloak will prompt for identity provider and credentials" -ForegroundColor Yellow
        Write-Host "   ??  Script will automatically continue when done" -ForegroundColor Yellow
        Write-Host ""
        
        # Wait for redirect to callback URL
        Write-Status "Waiting for authentication..."
        
        $authCode = $null
        $startTime = Get-Date
        
        while ((Get-Date) -lt $startTime.AddSeconds($TimeoutSeconds)) {
            $currentUrl = $driver.Url
            
            # Check if we've been redirected to our callback URL
            if ($currentUrl.StartsWith($RedirectUri)) {
                # Extract code from URL
                $uri = [System.Uri]$currentUrl
                $query = [System.Web.HttpUtility]::ParseQueryString($uri.Query)
                $authCode = $query["code"]
                
                if ($authCode) {
                    Write-Success "Authentication completed!"
                    break
                }
            }
            
            Start-Sleep -Seconds 2
        }
        
        if (-not $authCode) {
            throw "Authentication did not complete within $TimeoutSeconds seconds"
        }
        
        Write-Status "Closing browser..."
        $driver.Quit()
        $driver = $null
        
        return $authCode
        
    } catch {
        Write-Host "? Browser automation failed: $($_.Exception.Message)" -ForegroundColor Red
        
        # Check for redirect URI error specifically
        if ($_.Exception.Message -like "*Invalid parameter*" -or $_.Exception.Message -like "*redirect_uri*") {
            Write-Host ""
            Write-Host "?? This appears to be a redirect URI error!" -ForegroundColor Yellow
            Write-Host ""
            Write-Host "You need to add this redirect URI to your Keycloak client configuration:" -ForegroundColor Cyan
            Write-Host "  Client: grants-portal-5361" -ForegroundColor White
            Write-Host "  Redirect URI: $RedirectUri" -ForegroundColor White
            Write-Host ""
            Write-Host "Steps to fix:" -ForegroundColor Yellow
            Write-Host "1. Login to Keycloak Admin Console" -ForegroundColor White
            Write-Host "2. Go to Clients ? grants-portal-5361 ? Settings" -ForegroundColor White
            Write-Host "3. Add '$RedirectUri' to Valid Redirect URIs" -ForegroundColor White
            Write-Host "4. Save the configuration" -ForegroundColor White
            Write-Host ""
            
            # Offer to try a different redirect URI
            $tryDifferent = Read-Host "Would you like to try a different redirect URI? (Y/n)"
            if ($tryDifferent -eq "" -or $tryDifferent.ToLower() -eq "y") {
                $newRedirectUri = Get-ValidRedirectUris
                return Start-BrowserAuthentication -ClientId $ClientId -ClientSecret $ClientSecret -KeycloakUrl $KeycloakUrl -Realm $Realm -RedirectUri $newRedirectUri -TimeoutSeconds $TimeoutSeconds
            }
        }
        
        # Offer manual fallback
        Write-Host ""
        Write-Host "?? Falling back to manual authentication..." -ForegroundColor Yellow
        return Get-TokenManually -ClientId $ClientId -ClientSecret $ClientSecret -KeycloakUrl $KeycloakUrl -Realm $Realm -RedirectUri $RedirectUri
        
    } finally {
        if ($driver) {
            try {
                $driver.Quit()
            } catch {
                # Browser may already be closed
            }
        }
    }
}

function Get-TokenManually {
    param(
        [string]$ClientId,
        [string]$ClientSecret,
        [string]$KeycloakUrl,
        [string]$Realm,
        [string]$RedirectUri
    )
    
    Write-Host ""
    Write-Host "?? Manual Authentication" -ForegroundColor Yellow
    Write-Host "========================" -ForegroundColor Yellow
    Write-Host ""
    
    # Generate authorization URL
    $state = [System.Guid]::NewGuid().ToString()
    $authUrl = "$KeycloakUrl/realms/$Realm/protocol/openid-connect/auth?" +
               "client_id=$ClientId&" +
               "response_type=code&" +
               "scope=openid profile email&" +
               "redirect_uri=$([System.Web.HttpUtility]::UrlEncode($RedirectUri))&" +
               "state=$state"
    
    Write-Host "1. Open this URL in your browser:" -ForegroundColor Cyan
    Write-Host $authUrl -ForegroundColor White
    Write-Host ""
    Write-Host "2. Complete your authentication" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "3. After login, copy the 'code' parameter from the redirect URL" -ForegroundColor Cyan
    Write-Host "   (The URL should start with: $RedirectUri)" -ForegroundColor Gray
    Write-Host ""
    
    # Try to open browser automatically
    try {
        Start-Process $authUrl
        Write-Success "Browser opened automatically"
    } catch {
        Write-Info "Could not open browser automatically - please copy the URL above"
    }
    
    Write-Host ""
    $authCode = Read-Host "Enter the authorization code from the redirect URL"
    
    if ([string]::IsNullOrWhiteSpace($authCode)) {
        throw "No authorization code provided"
    }
    
    return $authCode
}

function Get-JwtToken {
    param(
        [string]$AuthCode,
        [string]$ClientId,
        [string]$ClientSecret,
        [string]$KeycloakUrl,
        [string]$Realm,
        [string]$RedirectUri
    )
    
    Write-Status "Exchanging authorization code for JWT token..."
    
    $tokenUrl = "$KeycloakUrl/realms/$Realm/protocol/openid-connect/token"
    $tokenBody = @{
        grant_type = "authorization_code"
        client_id = $ClientId
        client_secret = $ClientSecret
        code = $AuthCode
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
}

# Main execution
try {
    Write-Host ""
    Write-Host "?? Keycloak Token Generator" -ForegroundColor Cyan
    Write-Host "===========================" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Current directory: $(Get-Location)" -ForegroundColor Gray
    Write-Host ""
    
    # Get configuration (from parameters or interactively)
    if (-not $ClientId -or -not $ClientSecret -or -not $KeycloakUrl -or -not $Realm) {
        $config = Get-ConfigurationInteractively
        
        if (-not $ClientId) { $ClientId = $config.ClientId }
        if (-not $ClientSecret) { $ClientSecret = $config.ClientSecret }
        if (-not $KeycloakUrl) { $KeycloakUrl = $config.KeycloakUrl }
        if (-not $Realm) { $Realm = $config.Realm }
    }
    
    # Ask for redirect URI if not provided
    if (-not $RedirectUri -or $RedirectUri -eq "https://localhost:7000/auth/callback") {
        Write-Host "?? Using your app's callback endpoint by default" -ForegroundColor Yellow
        Write-Host "   If this doesn't work, try option 2 for manual mode" -ForegroundColor Gray
    }
    
    Write-Host "Ready to authenticate with:" -ForegroundColor Yellow
    Write-Host "  ?? $KeycloakUrl" -ForegroundColor White
    Write-Host "  ???  Realm: $Realm" -ForegroundColor White
    Write-Host "  ?? Client: $ClientId" -ForegroundColor White
    Write-Host "  ?? Redirect: $RedirectUri" -ForegroundColor White
    Write-Host ""
    
    $proceed = Read-Host "Continue? (Y/n)"
    if ($proceed -ne "" -and $proceed.ToLower() -ne "y") {
        Write-Host "Cancelled." -ForegroundColor Yellow
        exit 0
    }
    
    # Get authorization code (either automated or manual)
    $authCode = Start-BrowserAuthentication -ClientId $ClientId -ClientSecret $ClientSecret -KeycloakUrl $KeycloakUrl -Realm $Realm -RedirectUri $RedirectUri -TimeoutSeconds $TimeoutSeconds
    
    # Exchange code for token
    $tokenResult = Get-JwtToken -AuthCode $authCode -ClientId $ClientId -ClientSecret $ClientSecret -KeycloakUrl $KeycloakUrl -Realm $Realm -RedirectUri $RedirectUri
    
    Write-Host ""
    Write-Host "?? Success!" -ForegroundColor Green
    Write-Host "============" -ForegroundColor Green
    Write-Host ""
    
    # Save token
    $tokenFile = "keycloak-token.json"
    $tokenResult | ConvertTo-Json -Depth 10 | Out-File -FilePath $tokenFile -Encoding UTF8
    Write-Success "Token saved to: $tokenFile"
    
    # Set environment variable
    $env:KEYCLOAK_TOKEN = $tokenResult.AccessToken
    Write-Success "Token available as: `$env:KEYCLOAK_TOKEN"
    
    # Show expiration
    $expiresInMinutes = [math]::Round($tokenResult.ExpiresIn / 60, 1)
    Write-Host "? Expires in: $expiresInMinutes minutes" -ForegroundColor Yellow
    
    Write-Host ""
    Write-Host "?? Test your API:" -ForegroundColor Cyan
    Write-Host 'curl -H "Authorization: Bearer $env:KEYCLOAK_TOKEN" https://localhost:7000/Auth/userinfo' -ForegroundColor Gray
    Write-Host ""
    
} catch {
    Write-Host ""
    Write-Host "? Failed: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host ""
    Write-Host "?? Common Solutions:" -ForegroundColor Yellow
    Write-Host "  1. Add redirect URI to Keycloak client configuration:" -ForegroundColor White
    Write-Host "     https://localhost:7000/auth/callback" -ForegroundColor Gray
    Write-Host "  2. Make sure your API is running on https://localhost:7000" -ForegroundColor White
    Write-Host "  3. Install Chrome: https://www.google.com/chrome/" -ForegroundColor White
    Write-Host "  4. Install Edge: https://www.microsoft.com/edge" -ForegroundColor White
    Write-Host ""
    
    exit 1
}