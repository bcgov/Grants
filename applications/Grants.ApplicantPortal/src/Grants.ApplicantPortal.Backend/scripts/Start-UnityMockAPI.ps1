#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Starts the Unity Mock API for testing the Unity plugin

.DESCRIPTION
    This script starts the Unity Mock API which provides mock data endpoints
    that the Unity plugin can call during development and testing.

.PARAMETER Port
    The port number to run the mock API on (default: 5555)

.PARAMETER Environment
    The environment to run in (Development/Production, default: Development)

.EXAMPLE
    .\Start-UnityMockAPI.ps1
    Starts the Unity Mock API on the default port 5555

.EXAMPLE
    .\Start-UnityMockAPI.ps1 -Port 8080
    Starts the Unity Mock API on port 8080

.EXAMPLE
    .\Start-UnityMockAPI.ps1 -Port 5555 -Environment Production
    Starts the Unity Mock API on port 5555 in Production mode
#>

param(
    [Parameter(Mandatory = $false)]
    [int]$Port = 5555,
    
    [Parameter(Mandatory = $false)]
    [ValidateSet("Development", "Production")]
    [string]$Environment = "Development"
)

# Get the directory where this script is located
$ScriptDirectory = Split-Path -Parent $MyInvocation.MyCommand.Definition
$ProjectRoot = Split-Path -Parent $ScriptDirectory

# Unity Mock API project path
$UnityMockAPIPath = Join-Path $ProjectRoot "src\Grants.ApplicantPortal.API.Unity.MockAPI"

# Check if the project exists
if (-not (Test-Path $UnityMockAPIPath)) {
    Write-Error "Unity Mock API project not found at: $UnityMockAPIPath"
    Write-Host "Please ensure the Unity Mock API project has been created."
    exit 1
}

# Check if dotnet is available
try {
    $dotnetVersion = dotnet --version
    Write-Host "? Found .NET SDK version: $dotnetVersion" -ForegroundColor Green
}
catch {
    Write-Error "? .NET SDK not found. Please install .NET 9 SDK."
    exit 1
}

Write-Host "?? Starting Unity Mock API..." -ForegroundColor Yellow
Write-Host "?? Project Path: $UnityMockAPIPath" -ForegroundColor Gray
Write-Host "?? Port: $Port" -ForegroundColor Gray
Write-Host "?? Environment: $Environment" -ForegroundColor Gray
Write-Host ""

# Set environment variables
$env:ASPNETCORE_ENVIRONMENT = $Environment
$env:ASPNETCORE_URLS = "http://localhost:$Port"

# Change to the project directory
Set-Location $UnityMockAPIPath

try {
    Write-Host "?? Building Unity Mock API..." -ForegroundColor Yellow
    dotnet build --configuration Release
    
    if ($LASTEXITCODE -ne 0) {
        Write-Error "? Build failed!"
        exit 1
    }
    
    Write-Host "? Build successful!" -ForegroundColor Green
    Write-Host ""
    Write-Host "?? Unity Mock API will be available at:" -ForegroundColor Cyan
    Write-Host "   • API Base URL: http://localhost:$Port" -ForegroundColor White
    Write-Host "   • Swagger UI: http://localhost:$Port/swagger" -ForegroundColor White
    Write-Host "   • Health Check: http://localhost:$Port/health" -ForegroundColor White
    Write-Host ""
    Write-Host "?? Available Endpoints:" -ForegroundColor Cyan
    Write-Host "   • GET /api/app/applicant-profiles/tenants?ProfileId=&Subject=" -ForegroundColor White
    Write-Host "   • GET /api/app/applicant-profiles/profile?TenantId=&Key=&ProfileId=&Subject=" -ForegroundColor White
    Write-Host ""
    Write-Host "?? Supported Keys:" -ForegroundColor Cyan
    Write-Host "   CONTACTINFO, ADDRESSINFO, ORGINFO, SUBMISSIONINFO, PAYMENTINFO" -ForegroundColor White
    Write-Host ""
    Write-Host "??  Update your appsettings.Development.json to point Unity plugin to this endpoint!" -ForegroundColor Yellow
    Write-Host "   Set Plugins.UNITY.Configuration.BaseUrl to 'http://localhost:$Port'" -ForegroundColor Gray
    Write-Host ""
    Write-Host "?? Press Ctrl+C to stop the Unity Mock API" -ForegroundColor Red
    Write-Host ""
    
    # Start the application
    dotnet run --configuration Release --no-build
}
catch {
    Write-Error "? Failed to start Unity Mock API: $_"
    exit 1
}
finally {
    # Return to original directory
    Set-Location $ScriptDirectory
}