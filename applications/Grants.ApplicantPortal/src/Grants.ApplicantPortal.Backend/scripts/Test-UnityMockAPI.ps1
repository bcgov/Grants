#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Tests the Unity Mock API endpoints

.DESCRIPTION
    This script tests the Unity Mock API endpoints to ensure they are working correctly.
    It assumes the Unity Mock API is running on the specified port.

.PARAMETER Port
    The port number where the Unity Mock API is running (default: 5555)

.PARAMETER ProfileId
    The profile ID to use for testing (default: random GUID)

.EXAMPLE
    .\Test-UnityMockAPI.ps1
    Tests the Unity Mock API on the default port 5555

.EXAMPLE
    .\Test-UnityMockAPI.ps1 -Port 8080 -ProfileId "12345678-1234-1234-1234-123456789012"
    Tests the Unity Mock API on port 8080 with a specific profile ID
#>

param(
    [Parameter(Mandatory = $false)]
    [int]$Port = 5555,
    
    [Parameter(Mandatory = $false)]
    [string]$ProfileId = [System.Guid]::NewGuid().ToString()
)

$baseUrl = "http://localhost:$Port"

Write-Host "?? Testing Unity Mock API..." -ForegroundColor Yellow
Write-Host "?? Base URL: $baseUrl" -ForegroundColor Gray
Write-Host "?? Profile ID: $ProfileId" -ForegroundColor Gray
Write-Host ""

# Test health endpoint first
Write-Host "?? Testing health endpoint..." -ForegroundColor Cyan
try {
    $healthResponse = Invoke-RestMethod -Uri "$baseUrl/health" -Method Get -TimeoutSec 5
    Write-Host "? Health check successful: $($healthResponse.Status)" -ForegroundColor Green
}
catch {
    Write-Host "? Health check failed: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "?? Make sure the Unity Mock API is running on port $Port" -ForegroundColor Yellow
    exit 1
}

# Define headers for API key authentication
$headers = @{ "X-Api-Key" = "dev-unity-api-key" }
$testSubject = "test-subject%40azureidir"
$testTenantId = "DGP"

# Define endpoints to test — matches the real Unity API contract
$endpoints = @(
    @{ Path = "/api/app/applicant-profiles/tenants?ProfileId=$ProfileId&Subject=$testSubject"; Name = "Tenants" },
    @{ Path = "/api/app/applicant-profiles/profile?ProfileId=$ProfileId&Subject=$testSubject&TenantId=$testTenantId&Key=CONTACTINFO"; Name = "CONTACTINFO" },
    @{ Path = "/api/app/applicant-profiles/profile?ProfileId=$ProfileId&Subject=$testSubject&TenantId=$testTenantId&Key=ADDRESSINFO"; Name = "ADDRESSINFO" },
    @{ Path = "/api/app/applicant-profiles/profile?ProfileId=$ProfileId&Subject=$testSubject&TenantId=$testTenantId&Key=ORGINFO"; Name = "ORGINFO" },
    @{ Path = "/api/app/applicant-profiles/profile?ProfileId=$ProfileId&Subject=$testSubject&TenantId=$testTenantId&Key=SUBMISSIONINFO"; Name = "SUBMISSIONINFO" },
    @{ Path = "/api/app/applicant-profiles/profile?ProfileId=$ProfileId&Subject=$testSubject&TenantId=$testTenantId&Key=PAYMENTINFO"; Name = "PAYMENTINFO" }
)

Write-Host ""
Write-Host "?? Testing Unity API endpoints..." -ForegroundColor Cyan

$successCount = 0
$totalCount = $endpoints.Count

foreach ($endpoint in $endpoints) {
    try {
        Write-Host "  ? Testing $($endpoint.Name)..." -NoNewline
        
        $response = Invoke-RestMethod -Uri "$baseUrl$($endpoint.Path)" -Method Get -Headers $headers -TimeoutSec 10
        
        # Tenants returns an array; profile endpoint returns { profileId, subject, key, tenantId, data }
        if ($endpoint.Name -eq "Tenants") {
            if ($response -is [System.Array] -or $response.Count -gt 0) {
                Write-Host " ? (Tenants list)" -ForegroundColor Green
                $successCount++

                Write-Host ""
                Write-Host "  ?? Tenants Response:" -ForegroundColor Gray
                foreach ($tenant in $response) {
                    Write-Host "     - Id: $($tenant.id), Name: $($tenant.name)" -ForegroundColor White
                }
                Write-Host ""
            } else {
                Write-Host " ??  (Invalid structure)" -ForegroundColor Yellow
            }
        }
        elseif ($response -and $response.profileId -and $response.key -and $null -ne $response.data) {
            Write-Host " ? (key=$($response.key), tenantId=$($response.tenantId))" -ForegroundColor Green
            $successCount++
        } else {
            Write-Host " ??  (Invalid structure)" -ForegroundColor Yellow
        }
    }
    catch {
        Write-Host " ? ($($_.Exception.Message))" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "?? Test Results:" -ForegroundColor Cyan
Write-Host "   ? Successful: $successCount" -ForegroundColor Green
Write-Host "   ?? Total: $totalCount" -ForegroundColor Gray
Write-Host "   ?? Success Rate: $(([math]::Round(($successCount / $totalCount) * 100, 1)))%" -ForegroundColor $(if ($successCount -eq $totalCount) { "Green" } else { "Yellow" })

if ($successCount -eq $totalCount) {
    Write-Host ""
    Write-Host "?? All tests passed! Unity Mock API is working correctly." -ForegroundColor Green
    Write-Host "?? Unity API contract:" -ForegroundColor Cyan
    Write-Host "   Tenants:  GET /api/app/applicant-profiles/tenants?ProfileId=&Subject=" -ForegroundColor White
    Write-Host "   Profile:  GET /api/app/applicant-profiles/profile?TenantId=&Key=&ProfileId=&Subject=" -ForegroundColor White
    Write-Host "   Keys:     CONTACTINFO, ADDRESSINFO, ORGINFO, SUBMISSIONINFO, PAYMENTINFO" -ForegroundColor White
    Write-Host "   Response: { profileId, subject, key, tenantId, data: {} }" -ForegroundColor White
    Write-Host ""
    Write-Host "?? You can now configure your Unity plugin to use: $baseUrl" -ForegroundColor Cyan
} else {
    Write-Host ""
    Write-Host "??  Some tests failed. Check the Unity Mock API logs for more details." -ForegroundColor Yellow
}

Write-Host ""
Write-Host "?? Useful URLs:" -ForegroundColor Cyan
Write-Host "   • Swagger UI: $baseUrl/swagger" -ForegroundColor White
Write-Host "   • Health Check: $baseUrl/health" -ForegroundColor White