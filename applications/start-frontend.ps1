# Starts the Grants Angular frontend directly on this machine (no Docker) at http://localhost:4200.
# It calls the local backend at https://localhost:7000 (start it with .\start-backend.ps1).
$ErrorActionPreference = "Stop"
$Host.UI.RawUI.WindowTitle = "Grants Applicant Portal - Frontend Startup"

$applicationDirectory = Join-Path $PSScriptRoot "Grants.ApplicantPortal"
$frontendDirectory = Join-Path $applicationDirectory "src\Grants.ApplicantPortal.Frontend"

# Port 4200 is also published by the Docker grants-frontend container. Its host-side owner is
# Docker Desktop's port proxy - killing that takes Docker down, so stop the stack instead.
$dockerPortProcesses = @("com.docker.backend", "com.docker.proxy", "docker-proxy", "wslrelay", "vpnkit")
$connections = Get-NetTCPConnection -LocalPort 4200 -State Listen -ErrorAction SilentlyContinue
$processIds = $connections | Select-Object -ExpandProperty OwningProcess -Unique
foreach ($processId in $processIds) {
    $process = Get-Process -Id $processId -ErrorAction SilentlyContinue
    if (-not $process) { continue }
    if ($process.ProcessName -in $dockerPortProcesses) {
        throw "Port 4200 is in use by a Docker container (grants-frontend). Stop the Docker stack first: .\local-docker.ps1 stop"
    }
    Write-Host "Stopping $($process.ProcessName) (PID $processId) on port 4200..." -ForegroundColor Yellow
    Stop-Process -Id $processId -Force
}

if (-not (Test-Path (Join-Path $frontendDirectory "node_modules"))) {
    Write-Host "Installing frontend dependencies..." -ForegroundColor Cyan
    Push-Location $frontendDirectory
    try {
        npm.cmd install
        if ($LASTEXITCODE -ne 0) {
            throw "npm install failed with exit code $LASTEXITCODE."
        }
    }
    finally {
        Pop-Location
    }
}

Write-Host "Starting Angular frontend..." -ForegroundColor Green
Start-Process powershell.exe -ArgumentList @(
    "-NoProfile",
    "-NoExit",
    "-Command",
    "& { Set-Location '$frontendDirectory'; npm.cmd run start; if (`$LASTEXITCODE -ne 0) { Write-Error 'Angular frontend failed to start.' } }"
) -WorkingDirectory $frontendDirectory

Write-Host "Frontend startup command launched: http://localhost:4200 (backend: https://localhost:7000)" -ForegroundColor Cyan
