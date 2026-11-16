# Starts the Grants backend directly on this machine (no Docker) at https://localhost:7000.
# Uses Unity's shared Postgres container "unitydb" on localhost:5432 and the database name
# that matches the running OpenShift environments (ce395f-dev: dev and dev2): UnityApplicantPortal.
# The Postgres/Redis passwords are read from Unity's Unity.GrantManager\.env (repo next to this
# one, or $env:UNITY_REPO) and passed to dotnet run as environment variables.
#
# Like OpenShift (dbmigrator job, then API rollout), it first runs the DB migrator
# (API.Migrations: EF Core migrations + seed data) and only starts the API if that succeeds.
#   .\start-backend.ps1                  # migrate + seed, then start the API
#   .\start-backend.ps1 -SkipMigrations  # start the API only
param(
    [switch]$SkipMigrations
)

$ErrorActionPreference = "Stop"
$Host.UI.RawUI.WindowTitle = "Grants Applicant Portal - Backend Startup"

$applicationDirectory = Join-Path $PSScriptRoot "Grants.ApplicantPortal"
$backendDirectory = Join-Path $applicationDirectory "src\Grants.ApplicantPortal.Backend"
$apiDirectory = Join-Path $backendDirectory "src\Grants.ApplicantPortal.API.Web"
$migrationsDirectory = Join-Path $backendDirectory "src\Grants.ApplicantPortal.API.Migrations"
$databaseName = "UnityApplicantPortal"
$unityDbContainer = "unitydb"

$unityRepo = if ($env:UNITY_REPO) { $env:UNITY_REPO } else { Join-Path $PSScriptRoot "..\..\Unity" }
$unityEnvFile = [System.IO.Path]::GetFullPath((Join-Path $unityRepo "applications\Unity.GrantManager\.env"))
if (-not (Test-Path -PathType Leaf -LiteralPath $unityEnvFile)) {
    throw "Unity GrantManager .env not found at $unityEnvFile. Set `$env:UNITY_REPO to your Unity repo folder."
}
$unityEnv = @{}
foreach ($line in Get-Content -LiteralPath $unityEnvFile) {
    if ($line -match '^\s*([A-Za-z_][A-Za-z0-9_]*)\s*=\s*(.*)$') {
        $unityEnv[$Matches[1]] = $Matches[2].Trim().Trim('"', "'")
    }
}
if ([string]::IsNullOrWhiteSpace($unityEnv["UNITY_POSTGRES_PASSWORD"])) {
    throw "UNITY_POSTGRES_PASSWORD is not set in $unityEnvFile."
}
$dbUser = if ($unityEnv["UNITY_POSTGRES_USER"]) { $unityEnv["UNITY_POSTGRES_USER"] } else { "postgres" }
$dbPort = if ($unityEnv["UNITY_DB_PORT"]) { $unityEnv["UNITY_DB_PORT"] } else { "5432" }

$state = docker inspect --format "{{.State.Status}}" $unityDbContainer 2>$null
if ($state -ne "running") {
    throw "Shared Postgres container '$unityDbContainer' is not running. Start Unity's Unity.GrantManager stack first (docker compose up -d)."
}
$exists = docker exec $unityDbContainer psql -U postgres -Atc "select 1 from pg_database where datname = '$databaseName'" 2>$null
if ($exists -ne "1") {
    if ($SkipMigrations) {
        throw "Database '$databaseName' does not exist in '$unityDbContainer'. Run .\start-backend.ps1 without -SkipMigrations to create and seed it."
    }
    Write-Host "Database '$databaseName' does not exist yet; the migrator will create it." -ForegroundColor Cyan
}

$connections = Get-NetTCPConnection -LocalPort 7000 -State Listen -ErrorAction SilentlyContinue
$processIds = $connections | Select-Object -ExpandProperty OwningProcess -Unique
foreach ($processId in $processIds) {
    $process = Get-Process -Id $processId -ErrorAction SilentlyContinue
    if ($process) {
        Write-Host "Stopping $($process.ProcessName) (PID $processId) on port 7000..." -ForegroundColor Yellow
        Stop-Process -Id $processId -Force
    }
}

$redisConnection = "localhost:6379"
if ($unityEnv["REDIS_PASSWORD"]) { $redisConnection += ",password=$($unityEnv['REDIS_PASSWORD'])" }

# Pass connection strings through the inherited environment (not the command line, so the
# password is not visible in the process list). They override appsettings.*.json.
$previousGrants = $env:ConnectionStrings__Grants
$previousRedis = $env:ConnectionStrings__Redis
$previousEnvironment = $env:ASPNETCORE_ENVIRONMENT
try {
    $env:ConnectionStrings__Grants = "Host=localhost;Port=$dbPort;Database=$databaseName;Username=$dbUser;Password=$($unityEnv['UNITY_POSTGRES_PASSWORD'])"
    $env:ConnectionStrings__Redis = $redisConnection

    if (-not $SkipMigrations) {
        # The migrator loads appsettings*.json from the current directory, so run it from its project folder.
        Write-Host "Running DB migrator (migrations + seed) against $databaseName..." -ForegroundColor Green
        $env:ASPNETCORE_ENVIRONMENT = "Development"
        Push-Location $migrationsDirectory
        try {
            dotnet run
            if ($LASTEXITCODE -ne 0) {
                throw "DB migrator failed with exit code $LASTEXITCODE; the API was not started. See the output above."
            }
        }
        finally {
            Pop-Location
        }
        Write-Host "Database migrated and seeded." -ForegroundColor Green
    }

    Write-Host "Starting backend..." -ForegroundColor Green
    Start-Process powershell.exe -ArgumentList @(
        "-NoExit",
        "-Command",
        "Set-Location '$apiDirectory'; dotnet run"
    ) -WorkingDirectory $apiDirectory
} finally {
    $env:ConnectionStrings__Grants = $previousGrants
    $env:ConnectionStrings__Redis = $previousRedis
    $env:ASPNETCORE_ENVIRONMENT = $previousEnvironment
}

Write-Host "Backend startup command launched: https://localhost:7000" -ForegroundColor Cyan
Write-Host "Database: localhost:$dbPort / $databaseName (shared Unity container '$unityDbContainer')" -ForegroundColor Cyan
