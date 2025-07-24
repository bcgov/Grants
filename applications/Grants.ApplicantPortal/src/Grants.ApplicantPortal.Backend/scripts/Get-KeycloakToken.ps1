# Automated Keycloak Token Retrieval with PowerShell
# Requires Selenium WebDriver for browser automation

param(
    [Parameter(Mandatory=$true)]
    [string]$Username,
    
    [Parameter(Mandatory=$true)]
    [SecureString]$Password,
    
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
    [switch]$Headless = $true,
    
    [Parameter(Mandatory=$false)]
    [int]$TimeoutSeconds = 30
)

# Import required modules
try {
    Import-Module Selenium -ErrorAction Stop
} catch {
    Write-Error "Selenium module not found. Install it with: Install-Module -Name Selenium"
    exit 1
}

function Write-Status {
    param([string]$Message)
    Write-Host "?? $Message" -ForegroundColor Cyan
}

function Write-Success {
    param([string]$Message)
    Write-Host "? $Message" -ForegroundColor Green
}

function Write-Error {
    param([string]$Message)
    Write-Host "? $Message" -ForegroundColor Red
}

function Get-ConfigurationFromSecrets {
    Write-Status "Loading configuration from user secrets..."
    
    # Try to load from user secrets (dotnet user-secrets)
    $webProjectPath = "$PSScriptRoot\..\src\Grants.ApplicantPortal.API.Web"
    
    if (Test-Path $webProjectPath) {
        try {
            Push-Location $webProjectPath
            $secretsJson = dotnet user-secrets list --json 2>$null
            
            if ($secretsJson -and $secretsJson -ne "No secrets configured for this application.") {
                $secrets = $secretsJson | ConvertFrom-Json
                
                $config = @{
                    ClientId = $secrets.'Keycloak:Resource'
                    ClientSecret = $secrets.'Keycloak:Credentials:Secret'  
                    KeycloakUrl = $secrets.'Keycloak:AuthServerUrl'
                    Realm = $secrets.'Keycloak:Realm'
                }
                
                return $config
            }
        } catch {
            Write-Warning "Could not load from user secrets: $($_.Exception.Message)"
        } finally {
            Pop-Location
        }
    }
    
    # Fallback to environment variables
    Write-Status "Falling back to environment variables..."
    
    $config = @{
        ClientId = $env:KEYCLOAK_CLIENT_ID
        ClientSecret = $env:KEYCLOAK_CLIENT_SECRET
        KeycloakUrl = $env:KEYCLOAK_URL
        Realm = $env:KEYCLOAK_REALM
    }
    
    return $config
}

function Get-KeycloakTokenAutomated {
    param(
        [string]$Username,
        [SecureString]$Password,
        [string]$IdentityProvider,
        [string]$ClientId,
        [string]$ClientSecret,
        [string]$KeycloakUrl,
        [string]$Realm,
        [string]$RedirectUri,
        [bool]$Headless,
        [int]$TimeoutSeconds
    )
    
    $plainPassword = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto([System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($Password))
    
    Write-Status "Starting automated Keycloak authentication for $IdentityProvider..."
    
    # Setup Chrome options
    $chromeOptions = New-Object OpenQA.Selenium.Chrome.ChromeOptions
    if ($Headless) {
        $chromeOptions.AddArgument("--headless")
    }
    $chromeOptions.AddArgument("--disable-gpu")
    $chromeOptions.AddArgument("--no-sandbox")
    $chromeOptions.AddArgument("--disable-dev-shm-usage")
    $chromeOptions.AddArgument("--window-size=1920,1080")
    
    $driver = $null
    
    try {
        # Initialize Chrome driver
        Write-Status "Initializing Chrome WebDriver..."
        $driver = New-Object OpenQA.Selenium.Chrome.ChromeDriver($chromeOptions)
        $driver.Manage().Timeouts().ImplicitWait = [TimeSpan]::FromSeconds($TimeoutSeconds)
        
        # Step 1: Navigate to authorization URL
        $state = [System.Guid]::NewGuid().ToString()
        $authUrl = "$KeycloakUrl/realms/$Realm/protocol/openid-connect/auth?" +
                   "client_id=$ClientId&" +
                   "response_type=code&" +
                   "scope=openid profile email&" +
                   "redirect_uri=$([System.Web.HttpUtility]::UrlEncode($RedirectUri))&" +
                   "state=$state"
        
        Write-Status "Navigating to Keycloak authorization URL..."
        $driver.Navigate().GoToUrl($authUrl)
        
        # Step 2: Handle identity provider selection
        Write-Status "Selecting identity provider: $IdentityProvider..."
        
        # Wait for the page to load and look for identity provider buttons
        Start-Sleep -Seconds 2
        
        switch ($IdentityProvider.ToUpper()) {
            "IDIR" {
                $idirButton = $driver.FindElements([OpenQA.Selenium.By]::XPath("//a[contains(@href, 'idir') or contains(text(), 'IDIR') or contains(@class, 'idir')]"))
                if ($idirButton.Count -gt 0) {
                    $idirButton[0].Click()
                } else {
                    Write-Status "IDIR button not found, checking if already on IDIR login page..."
                }
            }
            "BCEID" {
                $bceidButton = $driver.FindElements([OpenQA.Selenium.By]::XPath("//a[contains(@href, 'bceid') or contains(text(), 'BCeID') or contains(@class, 'bceid')]"))
                if ($bceidButton.Count -gt 0) {
                    $bceidButton[0].Click()
                } else {
                    Write-Status "BCeID button not found, checking if already on BCeID login page..."
                }
            }
            "BCEID BUSINESS" {
                $bceidBusinessButton = $driver.FindElements([OpenQA.Selenium.By]::XPath("//a[contains(text(), 'BCeID Business') or contains(@href, 'bceidbusiness')]"))
                if ($bceidBusinessButton.Count -gt 0) {
                    $bceidBusinessButton[0].Click()
                } else {
                    Write-Status "BCeID Business button not found, trying general BCeID..."
                }
            }
        }
        
        # Wait for login form
        Start-Sleep -Seconds 3
        
        # Step 3: Fill in credentials
        Write-Status "Entering credentials..."
        
        # Try common username field selectors
        $usernameField = $null
        $usernameSelectors = @(
            "input[name='username']",
            "input[name='user']", 
            "input[name='userid']",
            "input[id='username']",
            "input[id='user']",
            "input[id='userid']",
            "input[type='text']",
            "input[type='email']"
        )
        
        foreach ($selector in $usernameSelectors) {
            try {
                $usernameField = $driver.FindElement([OpenQA.Selenium.By]::CssSelector($selector))
                if ($usernameField.Displayed) {
                    break
                }
            } catch {
                continue
            }
        }
        
        if ($null -eq $usernameField) {
            throw "Could not find username field on login page"
        }
        
        $usernameField.Clear()
        $usernameField.SendKeys($Username)
        
        # Try common password field selectors
        $passwordField = $null
        $passwordSelectors = @(
            "input[name='password']",
            "input[name='passwd']",
            "input[id='password']",
            "input[id='passwd']",
            "input[type='password']"
        )
        
        foreach ($selector in $passwordSelectors) {
            try {
                $passwordField = $driver.FindElement([OpenQA.Selenium.By]::CssSelector($selector))
                if ($passwordField.Displayed) {
                    break
                }
            } catch {
                continue
            }
        }
        
        if ($null -eq $passwordField) {
            throw "Could not find password field on login page"
        }
        
        $passwordField.Clear()
        $passwordField.SendKeys($plainPassword)
        
        # Step 4: Submit login form
        Write-Status "Submitting login form..."
        
        # Try to find and click submit button
        $submitButton = $null
        $submitSelectors = @(
            "input[type='submit']",
            "button[type='submit']",
            "button[name='submit']",
            "input[value*='Login']",
            "input[value*='Sign']",
            "button:contains('Login')",
            "button:contains('Sign')",
            ".btn-primary",
            ".login-button"
        )
        
        foreach ($selector in $submitSelectors) {
            try {
                $submitButton = $driver.FindElement([OpenQA.Selenium.By]::CssSelector($selector))
                if ($submitButton.Displayed -and $submitButton.Enabled) {
                    break
                }
            } catch {
                continue
            }
        }
        
        if ($null -ne $submitButton) {
            $submitButton.Click()
        } else {
            # Fallback: press Enter on password field
            $passwordField.SendKeys([OpenQA.Selenium.Keys]::Return)
        }
        
        # Step 5: Wait for redirect and extract authorization code
        Write-Status "Waiting for authentication and redirect..."
        
        $maxWaitTime = $TimeoutSeconds
        $waitTime = 0
        $authCode = $null
        
        while ($waitTime -lt $maxWaitTime) {
            $currentUrl = $driver.Url
            
            if ($currentUrl.StartsWith($RedirectUri)) {
                # Extract code from URL
                $uri = [System.Uri]$currentUrl
                $query = [System.Web.HttpUtility]::ParseQueryString($uri.Query)
                $authCode = $query["code"]
                
                if ($authCode) {
                    Write-Success "Authorization code obtained successfully!"
                    break
                }
            }
            
            # Check for error messages
            $errorElements = $driver.FindElements([OpenQA.Selenium.By]::XPath("//*[contains(text(), 'error') or contains(text(), 'Error') or contains(text(), 'invalid') or contains(text(), 'Invalid')]"))
            if ($errorElements.Count -gt 0) {
                $errorText = $errorElements[0].Text
                throw "Authentication error: $errorText"
            }
            
            Start-Sleep -Seconds 1
            $waitTime++
        }
        
        if (-not $authCode) {
            throw "Failed to obtain authorization code within $TimeoutSeconds seconds"
        }
        
        # Step 6: Exchange authorization code for tokens
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
        Write-Error "Error during automated authentication: $($_.Exception.Message)"
        throw
    } finally {
        if ($driver) {
            Write-Status "Closing browser..."
            $driver.Quit()
        }
        
        # Clear password from memory
        $plainPassword = $null
        [System.GC]::Collect()
    }
}

# Main execution
try {
    # Load configuration from user secrets or environment variables
    $config = Get-ConfigurationFromSecrets
    
    # Override with any provided parameters
    if ($ClientId) { $config.ClientId = $ClientId }
    if ($ClientSecret) { $config.ClientSecret = $ClientSecret }
    if ($KeycloakUrl) { $config.KeycloakUrl = $KeycloakUrl }
    if ($Realm) { $config.Realm = $Realm }
    
    # Validate required configuration
    if (-not $config.ClientId) {
        Write-Error "ClientId not found. Set it via user secrets (Keycloak:Resource) or environment variable KEYCLOAK_CLIENT_ID"
        exit 1
    }
    if (-not $config.ClientSecret) {
        Write-Error "ClientSecret not found. Set it via user secrets (Keycloak:Credentials:Secret) or environment variable KEYCLOAK_CLIENT_SECRET"
        exit 1
    }
    if (-not $config.KeycloakUrl) {
        Write-Error "KeycloakUrl not found. Set it via user secrets (Keycloak:AuthServerUrl) or environment variable KEYCLOAK_URL"
        exit 1
    }
    if (-not $config.Realm) {
        Write-Error "Realm not found. Set it via user secrets (Keycloak:Realm) or environment variable KEYCLOAK_REALM"
        exit 1
    }
    
    Write-Status "Using configuration:"
    Write-Host "  Client ID: $($config.ClientId)" -ForegroundColor White
    Write-Host "  Keycloak URL: $($config.KeycloakUrl)" -ForegroundColor White
    Write-Host "  Realm: $($config.Realm)" -ForegroundColor White
    Write-Host "  Client Secret: [PROTECTED]" -ForegroundColor White
    
    $tokenResult = Get-KeycloakTokenAutomated -Username $Username -Password $Password -IdentityProvider $IdentityProvider -ClientId $config.ClientId -ClientSecret $config.ClientSecret -KeycloakUrl $config.KeycloakUrl -Realm $config.Realm -RedirectUri $RedirectUri -Headless $Headless -TimeoutSeconds $TimeoutSeconds
    
    Write-Host ""
    Write-Success "?? Authentication completed successfully!"
    Write-Host ""
    Write-Host "Access Token:" -ForegroundColor Yellow
    Write-Host $tokenResult.AccessToken -ForegroundColor White
    Write-Host ""
    Write-Host "Token Details:" -ForegroundColor Yellow
    Write-Host "- Type: $($tokenResult.TokenType)" -ForegroundColor White
    Write-Host "- Expires In: $($tokenResult.ExpiresIn) seconds" -ForegroundColor White
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
    Write-Host "?? Test your API with:" -ForegroundColor Cyan
    Write-Host 'curl -H "Authorization: Bearer $env:KEYCLOAK_TOKEN" https://localhost:7000/Auth/userinfo' -ForegroundColor Gray
    
} catch {
    Write-Error "Script failed: $($_.Exception.Message)"
    exit 1
}