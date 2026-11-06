$ErrorActionPreference = "Stop"
$Host.UI.RawUI.WindowTitle = "Grants Applicant Portal - Frontend Startup"

$applicationDirectory = Join-Path $PSScriptRoot "Grants.ApplicantPortal"
$frontendDirectory = Join-Path $applicationDirectory "src\Grants.ApplicantPortal.Frontend"

$connections = Get-NetTCPConnection -LocalPort 4200 -State Listen -ErrorAction SilentlyContinue
$processIds = $connections | Select-Object -ExpandProperty OwningProcess -Unique
foreach ($processId in $processIds) {
    $process = Get-Process -Id $processId -ErrorAction SilentlyContinue
    if ($process) {
        Write-Host "Stopping $($process.ProcessName) (PID $processId) on port 4200..." -ForegroundColor Yellow
        Stop-Process -Id $processId -Force
    }
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

Write-Host "Frontend startup command launched: http://localhost:4200" -ForegroundColor Cyan