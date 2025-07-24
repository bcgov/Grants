# PowerShell Keycloak Token Automation

This directory contains PowerShell scripts to automate Keycloak token retrieval with IDIR/BCeID authentication.

## ?? Quick Start

### 1. Setup Configuration (One-time)
```powershell
# Set up your Keycloak configuration using user secrets
.\setup-dev-secrets.ps1

# Or manually set user secrets
cd src\Grants.ApplicantPortal.API.Web
dotnet user-secrets set "Keycloak:Resource" "your-client-id"
dotnet user-secrets set "Keycloak:Credentials:Secret" "your-client-secret"
dotnet user-secrets set "Keycloak:AuthServerUrl" "https://your-keycloak-server/auth"
dotnet user-secrets set "Keycloak:Realm" "your-realm"
```

### 2. Setup Dependencies (One-time)
```powershell
# Install Selenium WebDriver and other dependencies
.\Setup-TokenAutomation.ps1
```

### 3. Get Token (Automated)
```powershell
# Full automation with Selenium WebDriver
.\Get-KeycloakToken.ps1 -Username "your-username" -Password (Read-Host -AsSecureString) -IdentityProvider "IDIR"

# Simplified version (works with or without Selenium)
.\Get-KeycloakTokenSimple.ps1 -Username "your-username" -Password (Read-Host -AsSecureString) -IdentityProvider "IDIR"
```

### 4. Use Token
```powershell
# Token is automatically saved to environment variable
curl -H "Authorization: Bearer $env:KEYCLOAK_TOKEN" https://localhost:7000/Auth/userinfo
```

## ?? Scripts Overview

| Script | Purpose | Requirements |
|--------|---------|-------------|
| `setup-dev-secrets.ps1` | Configure Keycloak settings in user secrets | PowerShell 5+ |
| `Setup-TokenAutomation.ps1` | Installs dependencies (Selenium, ChromeDriver) | PowerShell 5+ |
| `Get-KeycloakToken.ps1` | Full browser automation | Selenium + Chrome |
| `Get-KeycloakTokenSimple.ps1` | Hybrid approach (auto or manual) | PowerShell only |

## ?? Configuration

### Method 1: User Secrets (Recommended)
User secrets are stored securely and not committed to source control:

```powershell
cd src\Grants.ApplicantPortal.API.Web
dotnet user-secrets set "Keycloak:Resource" "your-client-id"
dotnet user-secrets set "Keycloak:Credentials:Secret" "your-client-secret"
dotnet user-secrets set "Keycloak:AuthServerUrl" "https://your-keycloak-server/auth"
dotnet user-secrets set "Keycloak:Realm" "your-realm"
```

### Method 2: Environment Variables
Set these environment variables:

```powershell
$env:KEYCLOAK_CLIENT_ID = "your-client-id"
$env:KEYCLOAK_CLIENT_SECRET = "your-client-secret"
$env:KEYCLOAK_URL = "https://your-keycloak-server/auth"
$env:KEYCLOAK_REALM = "your-realm"
```

## ?? Parameters

### Common Parameters
- **`-Username`**: Your IDIR/BCeID username (required)
- **`-Password`**: SecureString password (required)
- **`-IdentityProvider`**: "IDIR", "BCeID", or "BCeID Business" (default: "IDIR")

### Advanced Parameters (Get-KeycloakToken.ps1)
- **`-Headless`**: Run browser in headless mode (default: true)
- **`-TimeoutSeconds`**: Authentication timeout (default: 30)
- **`-ClientId`**: Override client ID from configuration
- **`-ClientSecret`**: Override client secret from configuration

## ?? Usage Examples

### Basic Usage
```powershell
# IDIR authentication
$password = Read-Host -AsSecureString -Prompt "Enter your IDIR password"
.\Get-KeycloakTokenSimple.ps1 -Username "your-idir-username" -Password $password -IdentityProvider "IDIR"

# BCeID authentication  
$password = Read-Host -AsSecureString -Prompt "Enter your BCeID password"
.\Get-KeycloakTokenSimple.ps1 -Username "your-bceid-username" -Password $password -IdentityProvider "BCeID"
```

### Advanced Usage
```powershell
# Full automation with custom timeout
.\Get-KeycloakToken.ps1 -Username "username" -Password $securePassword -IdentityProvider "IDIR" -TimeoutSeconds 60 -Headless:$false

# View browser during automation (for debugging)
.\Get-KeycloakToken.ps1 -Username "username" -Password $securePassword -Headless:$false
```

### Batch Token Retrieval
```powershell
# Save credentials securely
$credential = Get-Credential -Message "Enter IDIR credentials"

# Get token
.\Get-KeycloakTokenSimple.ps1 -Username $credential.UserName -Password $credential.Password

# Test multiple API endpoints
$headers = @{ "Authorization" = "Bearer $env:KEYCLOAK_TOKEN" }
Invoke-RestMethod -Uri "https://localhost:7000/Auth/userinfo" -Headers $headers
Invoke-RestMethod -Uri "https://localhost:7000/api/profiles/123" -Headers $headers
```

## ?? Security Notes

### Configuration Security
- **User secrets**: Stored in `%APPDATA%\Microsoft\UserSecrets\grants-applicant-portal-web-secrets\secrets.json`
- **Environment variables**: Stored in current session only
- **Never commit secrets** to source control

### Credential Handling
- **Always use SecureString**: `Read-Host -AsSecureString`
- **Never hardcode passwords** in scripts
- **Use Get-Credential** for interactive scenarios
- **Clear variables** after use: `$password = $null`

### Token Storage
- Tokens are saved to `keycloak-token.json`
- Environment variable `KEYCLOAK_TOKEN` is set
- Tokens expire (typically 5-15 minutes)
- Refresh tokens last longer (30+ minutes)

## ??? Dependencies

### Required
- **PowerShell 5.0+** (Windows PowerShell or PowerShell Core)
- **Internet connection** to reach Keycloak server
- **Keycloak configuration** (via user secrets or environment variables)

### Optional (for full automation)
- **Selenium WebDriver module**: `Install-Module -Name Selenium`
- **Chrome browser** or **Microsoft Edge**
- **ChromeDriver** or **EdgeDriver**

### Installation
```powershell
# Install Selenium module
Install-Module -Name Selenium -Force -Scope CurrentUser

# Or run the setup script
.\Setup-TokenAutomation.ps1
```

## ?? Troubleshooting

### Common Issues

#### "Configuration not found"
Make sure you've set up your configuration:
```powershell
# Check user secrets
cd src\Grants.ApplicantPortal.API.Web
dotnet user-secrets list

# Or set environment variables
$env:KEYCLOAK_CLIENT_ID = "your-client-id"
```

#### "Selenium module not found"
```powershell
Install-Module -Name Selenium -Force -Scope CurrentUser
```

#### "ChromeDriver not found" 
- Download from: https://chromedriver.chromium.org/
- Extract to a folder in your PATH
- Or run `.\Setup-TokenAutomation.ps1`

#### "Could not find username field"
- Try running with `-Headless:$false` to see the login page
- Different identity providers may have different form layouts
- The script attempts multiple field selectors

#### "Authentication timeout"
- Increase timeout: `-TimeoutSeconds 60`
- Check network connectivity to Keycloak server
- Verify username/password are correct

#### "Invalid redirect URI"
- Ensure `http://localhost:8080/callback` is configured in Keycloak
- The redirect URI must match exactly

### Debug Mode
```powershell
# Run with visible browser to debug
.\Get-KeycloakToken.ps1 -Username "user" -Password $pass -Headless:$false -TimeoutSeconds 120
```

### Manual Fallback
If automation fails, the simple script provides manual instructions:
```powershell
.\Get-KeycloakTokenSimple.ps1 -Username "user" -Password $pass
# Follow the manual steps if Selenium is not available
```

## ?? Testing Your Token

Once you have a token, test it with your API:

```powershell
# Check token validity
$headers = @{ "Authorization" = "Bearer $env:KEYCLOAK_TOKEN" }

# Get user info
$userInfo = Invoke-RestMethod -Uri "https://localhost:7000/Auth/userinfo" -Headers $headers
Write-Host "Logged in as: $($userInfo.username)" -ForegroundColor Green

# Test protected endpoints
try {
    $profiles = Invoke-RestMethod -Uri "https://localhost:7000/api/profiles" -Headers $headers
    Write-Host "? API access successful" -ForegroundColor Green
} catch {
    Write-Host "? API access failed: $($_.Exception.Message)" -ForegroundColor Red
}
```

## ?? Token Refresh

```powershell
# If you have a refresh token, you can get a new access token
$tokenData = Get-Content "keycloak-token.json" | ConvertFrom-Json
$refreshToken = $tokenData.refresh_token

# Load configuration (same as in scripts)
cd src\Grants.ApplicantPortal.API.Web
$secretsJson = dotnet user-secrets list --json
$secrets = $secretsJson | ConvertFrom-Json

$body = @{
    grant_type = "refresh_token"
    client_id = $secrets.'Keycloak:Resource'
    client_secret = $secrets.'Keycloak:Credentials:Secret'
    refresh_token = $refreshToken
}

$newTokens = Invoke-RestMethod -Uri "$($secrets.'Keycloak:AuthServerUrl')/realms/$($secrets.'Keycloak:Realm')/protocol/openid-connect/token" -Method Post -Body $body -ContentType "application/x-www-form-urlencoded"

$env:KEYCLOAK_TOKEN = $newTokens.access_token
Write-Host "? Token refreshed successfully" -ForegroundColor Green
```

## ?? Automation Tips

### Scheduled Token Retrieval
```powershell
# Store configuration in user secrets (secure)
# Store credentials separately (use Windows Credential Manager or similar)
$credential = Get-StoredCredential -Target "MyIDIRAccount"
.\Get-KeycloakTokenSimple.ps1 -Username $credential.UserName -Password $credential.Password
```

### CI/CD Integration
```powershell
# Use environment variables for automation
$username = $env:IDIR_USERNAME
$password = ConvertTo-SecureString $env:IDIR_PASSWORD -AsPlainText -Force

.\Get-KeycloakToken.ps1 -Username $username -Password $password -IdentityProvider "IDIR"
```

## ??? Security Best Practices

1. **Never commit secrets** to source control
2. **Use user secrets** for local development  
3. **Use environment variables** for CI/CD
4. **Clear sensitive variables** after use
5. **Use SecureString** for password input
6. **Rotate secrets** regularly
7. **Monitor token usage** and expiration

Your PowerShell automation is now secure and ready! ??