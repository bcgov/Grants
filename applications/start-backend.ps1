$ErrorActionPreference = "Stop"
$Host.UI.RawUI.WindowTitle = "Grants Applicant Portal - Backend Startup"

$applicationDirectory = Join-Path $PSScriptRoot "Grants.ApplicantPortal"
$backendDirectory = Join-Path $applicationDirectory "src\Grants.ApplicantPortal.Backend"
$apiDirectory = Join-Path $backendDirectory "src\Grants.ApplicantPortal.API.Web"

$connections = Get-NetTCPConnection -LocalPort 7000 -State Listen -ErrorAction SilentlyContinue
$processIds = $connections | Select-Object -ExpandProperty OwningProcess -Unique
foreach ($processId in $processIds) {
    $process = Get-Process -Id $processId -ErrorAction SilentlyContinue
    if ($process) {
        Write-Host "Stopping $($process.ProcessName) (PID $processId) on port 7000..." -ForegroundColor Yellow
        Stop-Process -Id $processId -Force
    }
}

Write-Host "Starting backend..." -ForegroundColor Green
Start-Process powershell.exe -ArgumentList @(
    "-NoExit",
    "-Command",
    "Set-Location '$apiDirectory'; dotnet run"
) -WorkingDirectory $apiDirectory

Write-Host "Backend startup command launched: https://localhost:7000" -ForegroundColor Cyan