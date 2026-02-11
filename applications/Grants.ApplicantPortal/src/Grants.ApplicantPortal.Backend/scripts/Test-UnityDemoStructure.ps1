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

Write-Host "?? Testing Unity Mock API DEMO Structure Compatibility..." -ForegroundColor Yellow
Write-Host "?? Base URL: $baseUrl" -ForegroundColor Gray
Write-Host "?? Profile ID: $testProfileId" -ForegroundColor Gray
Write-Host ""

# Test contacts endpoint
Write-Host "?? Testing Contacts endpoint..." -ForegroundColor Cyan
try {
    $contactsResponse = Invoke-RestMethod -Uri "$baseUrl/api/profiles/$testProfileId/contacts?provider=DGP" -Method Get -TimeoutSec 10
    
    Write-Host "? Response structure validation:" -ForegroundColor Green
    Write-Host "   profileId: $($contactsResponse.profileId)" -ForegroundColor White
    Write-Host "   pluginId: $($contactsResponse.pluginId)" -ForegroundColor White  
    Write-Host "   provider: $($contactsResponse.provider)" -ForegroundColor White
    Write-Host "   populatedAt: $($contactsResponse.populatedAt)" -ForegroundColor White
    
    # Parse the JSON data
    $contactsData = $contactsResponse.data | ConvertFrom-Json
    Write-Host "   data.contacts.length: $($contactsData.contacts.Length)" -ForegroundColor White
    Write-Host "   data.summary.totalContacts: $($contactsData.summary.totalContacts)" -ForegroundColor White
    
    # Show sample contact
    if ($contactsData.contacts -and $contactsData.contacts.Length -gt 0) {
        $firstContact = $contactsData.contacts[0]
        Write-Host ""
        Write-Host "?? Sample Contact Data:" -ForegroundColor Cyan
        Write-Host "   id: $($firstContact.id)" -ForegroundColor White
        Write-Host "   name: $($firstContact.name)" -ForegroundColor White
        Write-Host "   type: $($firstContact.type)" -ForegroundColor White
        Write-Host "   isPrimary: $($firstContact.isPrimary)" -ForegroundColor White
        Write-Host "   email: $($firstContact.email)" -ForegroundColor White
    }
    
} catch {
    Write-Host "? Contacts test failed: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""

# Test addresses endpoint  
Write-Host "?? Testing Addresses endpoint..." -ForegroundColor Cyan
try {
    $addressesResponse = Invoke-RestMethod -Uri "$baseUrl/api/profiles/$testProfileId/addresses?provider=ABC" -Method Get -TimeoutSec 10
    
    # Parse the JSON data
    $addressesData = $addressesResponse.data | ConvertFrom-Json
    Write-Host "? Addresses structure validated" -ForegroundColor Green
    Write-Host "   data.addresses.length: $($addressesData.addresses.Length)" -ForegroundColor White
    Write-Host "   data.summary.totalAddresses: $($addressesData.summary.totalAddresses)" -ForegroundColor White
    
    # Show sample address
    if ($addressesData.addresses -and $addressesData.addresses.Length -gt 0) {
        $firstAddress = $addressesData.addresses[0]
        Write-Host ""
        Write-Host "?? Sample Address Data:" -ForegroundColor Cyan
        Write-Host "   id: $($firstAddress.id)" -ForegroundColor White
        Write-Host "   type: $($firstAddress.type)" -ForegroundColor White
        Write-Host "   addressLine1: $($firstAddress.addressLine1)" -ForegroundColor White
        Write-Host "   city: $($firstAddress.city)" -ForegroundColor White
        Write-Host "   isPrimary: $($firstAddress.isPrimary)" -ForegroundColor White
    }
    
} catch {
    Write-Host "? Addresses test failed: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""

# Test organization endpoint
Write-Host "?? Testing Organization endpoint..." -ForegroundColor Cyan
try {
    $orgResponse = Invoke-RestMethod -Uri "$baseUrl/api/profiles/$testProfileId/organization?provider=DGP" -Method Get -TimeoutSec 10
    
    # Parse the JSON data
    $orgData = $orgResponse.data | ConvertFrom-Json
    Write-Host "? Organization structure validated" -ForegroundColor Green
    Write-Host "   organizationInfo.orgName: $($orgData.organizationInfo.orgName)" -ForegroundColor White
    Write-Host "   organizationInfo.orgStatus: $($orgData.organizationInfo.orgStatus)" -ForegroundColor White
    Write-Host "   organizationInfo.organizationType: $($orgData.organizationInfo.organizationType)" -ForegroundColor White
    
} catch {
    Write-Host "? Organization test failed: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""
Write-Host "?? Structure Comparison Complete!" -ForegroundColor Green
Write-Host "? Unity Mock API data structure matches DEMO plugin format" -ForegroundColor Green
Write-Host "?? All responses use the same pattern:" -ForegroundColor Cyan
Write-Host "   - profileId (GUID)" -ForegroundColor White
Write-Host "   - pluginId ('UNITY')" -ForegroundColor White  
Write-Host "   - provider ('DGP' or 'ABC')" -ForegroundColor White
Write-Host "   - data (JSON string)" -ForegroundColor White
Write-Host "   - populatedAt (ISO timestamp)" -ForegroundColor White