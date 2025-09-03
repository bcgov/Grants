# Quick setup script for developer secrets
# Run this from the repository root directory

Write-Host "Setting up Grants Applicant Portal API secrets..." -ForegroundColor Green

# Navigate to the Web project directory
$webProjectPath = "src\Grants.ApplicantPortal.API.Web"

if (-not (Test-Path $webProjectPath)) {
    Write-Host "? Web project directory not found: $webProjectPath" -ForegroundColor Red
    Write-Host "   Make sure you're running this script from the repository root directory." -ForegroundColor Yellow
    exit 1
}

try {
    Push-Location $webProjectPath
    
    # Initialize user secrets if not already done
    Write-Host "Initializing user secrets..." -ForegroundColor Yellow
    dotnet user-secrets init
    
    # Get secrets from user input
    Write-Host ""
    Write-Host "Please provide your Keycloak configuration:" -ForegroundColor Cyan
    Write-Host ""
    
    $clientId = Read-Host "Client ID (Resource)"
    $clientSecret = Read-Host "Client Secret" -AsSecureString
    $authServerUrl = Read-Host "Auth Server URL (e.g., https://dev.loginproxy.gov.bc.ca/auth)"
    $realm = Read-Host "Realm (e.g., standard)"
    
    # Convert SecureString to plain text for dotnet user-secrets
    $clientSecretPlain = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto([System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($clientSecret))
    
    # Set Keycloak configuration
    Write-Host ""
    Write-Host "Setting Keycloak configuration..." -ForegroundColor Yellow
    
    dotnet user-secrets set "Keycloak:Resource" $clientId
    dotnet user-secrets set "Keycloak:Credentials:Secret" $clientSecretPlain
    dotnet user-secrets set "Keycloak:AuthServerUrl" $authServerUrl
    dotnet user-secrets set "Keycloak:Realm" $realm
    
    # Clear the plain text secret from memory
    $clientSecretPlain = $null
    [System.GC]::Collect()
    
    Write-Host ""
    Write-Host "? Secrets configured successfully!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Configured secrets:" -ForegroundColor Cyan
    dotnet user-secrets list
    
    Write-Host ""
    Write-Host "? Setup complete! You can now:" -ForegroundColor Green
    Write-Host "   1. Run the application: dotnet run" -ForegroundColor White
    Write-Host "   2. Use token automation scripts: .\scripts\Get-KeycloakTokenSimple.ps1" -ForegroundColor White
    
} catch {
    Write-Host "? Error setting up secrets: $($_.Exception.Message)" -ForegroundColor Red
} finally {
    Pop-Location
}
