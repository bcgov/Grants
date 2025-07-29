# PowerShell Keycloak Token Automation ??

This directory contains PowerShell scripts to automate Keycloak token retrieval with IDIR/BCeID authentication, featuring a beautiful API callback experience.

## ?? Quick Start

### 1. Setup Configuration (One-time)
```powershell
# Set up your Keycloak configuration using user secrets
.\setup-dev-secrets.ps1
```

### 2. Get Token (Recommended - Fully Automated)
```powershell
# Start your API first
cd src\Grants.ApplicantPortal.API.Web
dotnet run

# Then run the fully automated script (opens beautiful callback page)
.\scripts\Get-KeycloakTokenFullyAutomated.ps1
```

### 3. Use Token
```powershell
# Token is automatically saved to environment variable
curl -H "Authorization: Bearer $env:KEYCLOAK_TOKEN" https://localhost:7000/Auth/userinfo
```

## ?? Available Scripts

| Script | Description | Experience |
|--------|-------------|------------|
| **`Get-KeycloakTokenFullyAutomated.ps1`** ? | Opens browser, handles login, shows beautiful callback page with copy buttons | **Best** - Fully automated |
| **`Get-KeycloakTokenSimple.ps1`** | Selenium-powered browser automation with fallback to manual | **Good** - Reliable fallback |
| **`setup-dev-secrets.ps1`** | Configure Keycloak settings in user secrets | **Setup** - One-time |

## ?? Recommended Workflow

### Fully Automated Experience (NEW!)
The best way to get tokens now uses your API's beautiful callback endpoint:

```powershell
# 1. Start your API (this serves the callback endpoint)
cd src\Grants.ApplicantPortal.API.Web
dotnet run

# 2. Run the fully automated script
.\scripts\Get-KeycloakTokenFullyAutomated.ps1
```

**What happens:**
1. ?? **Browser opens** to Keycloak login
2. ?? **You complete authentication** (IDIR, BCeID, etc.)
3. ? **Beautiful callback page appears** with:
   - ??? Access token with copy button
   - ?? Refresh token with copy button  
   - ?? User information display
   - ? Token expiration details
   - ?? Ready-to-use curl examples

### Simple Automation (Fallback)
If the fully automated approach doesn't work:

```powershell
.\scripts\Get-KeycloakTokenSimple.ps1
```

This uses Selenium browser automation and works without requiring your API to be running.

## ?? Configuration

### User Secrets (Recommended)
User secrets are stored securely and not committed to source control:

```powershell
cd src\Grants.ApplicantPortal.API.Web
dotnet user-secrets set "Keycloak:Resource" "grants-portal-5361"
dotnet user-secrets set "Keycloak:Credentials:Secret" "your-client-secret"
dotnet user-secrets set "Keycloak:AuthServerUrl" "https://dev.loginproxy.gov.bc.ca/auth"
dotnet user-secrets set "Keycloak:Realm" "standard"
```

## ?? Usage Examples

### Most Common Usage
```powershell
# Easy setup - just run this once
.\scripts\setup-dev-secrets.ps1

# Then get tokens anytime with:
.\scripts\Get-KeycloakTokenFullyAutomated.ps1
```

### Advanced Usage
```powershell
# If you want to see the browser automation in action
.\scripts\Get-KeycloakTokenSimple.ps1

# Test your token immediately
$headers = @{ "Authorization" = "Bearer $env:KEYCLOAK_TOKEN" }
Invoke-RestMethod -Uri "https://localhost:7000/Auth/userinfo" -Headers $headers
```

## ??? Security Notes

### Configuration Security
- **User secrets**: Stored in `%APPDATA%\Microsoft\UserSecrets\grants-applicant-portal-web-secrets\secrets.json`
- **Never commit secrets** to source control
- **Secrets are loaded automatically** by both scripts

### Token Security
- **Tokens are temporary** (typically 5-15 minutes)
- **Automatic environment variable**: `$env:KEYCLOAK_TOKEN`
- **JSON file saved**: `keycloak-token.json` for backup
- **Copy buttons** make it easy to use tokens securely

## ?? Dependencies

### Required
- **PowerShell 5.0+** (Windows PowerShell or PowerShell Core)
- **Internet connection** to reach Keycloak server
- **Keycloak configuration** (via user secrets)

### For Fully Automated Script (Recommended)
- **Your API running** on `https://localhost:7000`
- **Default browser** (any modern browser works)

### For Simple Script (Fallback)
- **Selenium WebDriver module**: Auto-installed if missing
- **Chrome or Edge browser** for automation

## ?? Troubleshooting

### "API server not running" (Fully Automated)
```powershell
# Make sure your API is running first
cd src\Grants.ApplicantPortal.API.Web
dotnet run
# Wait for "Now listening on: https://localhost:7000"
```

### "Configuration not found"
```powershell
# Run the setup script
.\scripts\setup-dev-secrets.ps1

# Or check your user secrets
cd src\Grants.ApplicantPortal.API.Web
dotnet user-secrets list
```

### "Selenium module not found" (Simple Script)
The script will automatically try to install Selenium. If it fails:
```powershell
Install-Module -Name Selenium -Force -Scope CurrentUser
```

### "Invalid redirect URI"
Make sure this redirect URI is configured in your Keycloak client:
- **For Fully Automated**: `https://localhost:7000/auth/callback`
- **For Simple Script**: `https://localhost:7000/auth/callback` (first choice)

## ?? Testing Your Token

Once you have a token:

```powershell
# Check token validity and user info
$headers = @{ "Authorization" = "Bearer $env:KEYCLOAK_TOKEN" }
$userInfo = Invoke-RestMethod -Uri "https://localhost:7000/Auth/userinfo" -Headers $headers
Write-Host "? Logged in as: $($userInfo.username)" -ForegroundColor Green

# Test other protected endpoints
Invoke-RestMethod -Uri "https://localhost:7000/System/info" -Headers $headers
```

## ?? Pro Tips

### Quick Token Refresh
```powershell
# Just re-run the fully automated script - it's fast!
.\scripts\Get-KeycloakTokenFullyAutomated.ps1
```

### Multiple Environment Support
```powershell
# Different Keycloak configurations for different environments
.\scripts\Get-KeycloakTokenFullyAutomated.ps1 -KeycloakUrl "https://test.loginproxy.gov.bc.ca/auth" -Realm "test"
```

### Integration with Other Tools
```powershell
# Export token for Postman, curl, etc.
Write-Host "Authorization: Bearer $env:KEYCLOAK_TOKEN"

# Or copy directly to clipboard (Windows)
"Bearer $env:KEYCLOAK_TOKEN" | Set-Clipboard
```

## ?? What Makes This Special

1. **?? Fully Automated**: No manual token copying from URLs
2. **?? Beautiful UI**: Professional callback page with copy buttons
3. **?? User-Friendly**: Shows user info, expiration, examples
4. **?? Smart Fallback**: Multiple approaches if one doesn't work
5. **??? Secure**: Uses user secrets, no hardcoded credentials
6. **? Fast**: Quick token refresh workflow

Your PowerShell automation is now streamlined and ready! The fully automated approach with the beautiful callback page makes getting Keycloak tokens a pleasant experience. ??