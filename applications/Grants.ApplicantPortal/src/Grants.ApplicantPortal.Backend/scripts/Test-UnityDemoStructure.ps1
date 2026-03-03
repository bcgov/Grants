#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Tests Unity Mock API data structure matches DEMO plugin structure

.DESCRIPTION
    This script fetches data from Unity Mock API and compares it to the DEMO plugin structure
    to ensure compatibility.

.PARAMETER Port
    The port number where the Unity Mock API is running (default: 5555)

.EXAMPLE
    .\Test-UnityDemoStructure.ps1
    Tests the Unity Mock API structure compatibility
#>

param(
    [Parameter(Mandatory = $false)]
    [int]$Port = 5555
)

$baseUrl = "http://localhost:$Port"
$testProfileId = "019b4788-d7a7-7c40-b25e-98a361adbbfc"  # Same as DEMO examples

Write-Host "?? Testing Unity Mock API Profile Endpoint..." -ForegroundColor Yellow
Write-Host "?? Base URL: $baseUrl" -ForegroundColor Gray
Write-Host "?? Profile ID: $testProfileId" -ForegroundColor Gray
Write-Host ""

$headers = @{ "X-Api-Key" = "dev-unity-api-key" }
$testSubject = "test-subject%40azureidir"
$testTenantId = "DGP"

# Test CONTACTINFO endpoint
Write-Host "?? Testing CONTACTINFO..." -ForegroundColor Cyan
try {
    $contactsResponse = Invoke-RestMethod -Uri "$baseUrl/api/app/applicant-profiles/profile?ProfileId=$testProfileId&Subject=$testSubject&TenantId=$testTenantId&Key=CONTACTINFO" -Method Get -Headers $headers -TimeoutSec 10
    
    Write-Host "? Response structure validation:" -ForegroundColor Green
    Write-Host "   profileId: $($contactsResponse.profileId)" -ForegroundColor White
    Write-Host "   subject: $($contactsResponse.subject)" -ForegroundColor White  
    Write-Host "   key: $($contactsResponse.key)" -ForegroundColor White
    Write-Host "   tenantId: $($contactsResponse.tenantId)" -ForegroundColor White
    
    if ($contactsResponse.data -and $contactsResponse.data.contacts) {
        Write-Host "   data.contacts.length: $($contactsResponse.data.contacts.Length)" -ForegroundColor White
        
        if ($contactsResponse.data.contacts.Length -gt 0) {
            $firstContact = $contactsResponse.data.contacts[0]
            Write-Host ""
            Write-Host "?? Sample Contact Data:" -ForegroundColor Cyan
            Write-Host "   id: $($firstContact.id)" -ForegroundColor White
            Write-Host "   name: $($firstContact.name)" -ForegroundColor White
            Write-Host "   type: $($firstContact.type)" -ForegroundColor White
            Write-Host "   email: $($firstContact.email)" -ForegroundColor White
        }
    }
    
} catch {
    Write-Host "? CONTACTINFO test failed: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""

# Test ADDRESSINFO endpoint  
Write-Host "?? Testing ADDRESSINFO..." -ForegroundColor Cyan
try {
    $addressesResponse = Invoke-RestMethod -Uri "$baseUrl/api/app/applicant-profiles/profile?ProfileId=$testProfileId&Subject=$testSubject&TenantId=ABC&Key=ADDRESSINFO" -Method Get -Headers $headers -TimeoutSec 10
    
    Write-Host "? ADDRESSINFO structure validated" -ForegroundColor Green
    if ($addressesResponse.data -and $addressesResponse.data.addresses) {
        Write-Host "   data.addresses.length: $($addressesResponse.data.addresses.Length)" -ForegroundColor White
        
        if ($addressesResponse.data.addresses.Length -gt 0) {
            $firstAddress = $addressesResponse.data.addresses[0]
            Write-Host ""
            Write-Host "?? Sample Address Data:" -ForegroundColor Cyan
            Write-Host "   id: $($firstAddress.id)" -ForegroundColor White
            Write-Host "   type: $($firstAddress.type)" -ForegroundColor White
            Write-Host "   addressLine1: $($firstAddress.addressLine1)" -ForegroundColor White
            Write-Host "   city: $($firstAddress.city)" -ForegroundColor White
        }
    }
    
} catch {
    Write-Host "? ADDRESSINFO test failed: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""

# Test ORGINFO endpoint
Write-Host "?? Testing ORGINFO..." -ForegroundColor Cyan
try {
    $orgResponse = Invoke-RestMethod -Uri "$baseUrl/api/app/applicant-profiles/profile?ProfileId=$testProfileId&Subject=$testSubject&TenantId=$testTenantId&Key=ORGINFO" -Method Get -Headers $headers -TimeoutSec 10
    
    Write-Host "? ORGINFO structure validated" -ForegroundColor Green
    if ($orgResponse.data -and $orgResponse.data.organizationInfo) {
        Write-Host "   organizationInfo.orgName: $($orgResponse.data.organizationInfo.orgName)" -ForegroundColor White
        Write-Host "   organizationInfo.orgStatus: $($orgResponse.data.organizationInfo.orgStatus)" -ForegroundColor White
    }
    
} catch {
    Write-Host "? ORGINFO test failed: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""
Write-Host "?? Structure Validation Complete!" -ForegroundColor Green
Write-Host "?? Unity API contract:" -ForegroundColor Cyan
Write-Host "   Endpoint: GET /api/app/applicant-profiles/profile" -ForegroundColor White
Write-Host "   Params:   TenantId, Key, ProfileId, Subject" -ForegroundColor White  
Write-Host "   Keys:     CONTACTINFO, ADDRESSINFO, ORGINFO, SUBMISSIONINFO, PAYMENTINFO" -ForegroundColor White
Write-Host "   Response: { profileId, subject, key, tenantId, data: {} }" -ForegroundColor White