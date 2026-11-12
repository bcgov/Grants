# Grants Application - Local Development Environment (Docker) PowerShell Script
#
# Database names match the running OpenShift environments (ce395f-dev: dev and dev2):
#   UnityApplicantPortal - Grants Applicant Portal
#
# Two modes, picked automatically:
#   Shared (recommended) - Grants.ApplicantPortal\docker-compose.override.yml exists (copy it from
#       docker-compose.override.yml.example). Grants uses Unity's shared Postgres container "unitydb"
#       (localhost:5432) and Unity's Redis, like ce395f-dev where all apps share one Postgres cluster.
#       Requires Unity's Unity.GrantManager stack running (repo next to this one, or $env:UNITY_REPO).
#   Standalone - no override file. Grants starts its own Postgres on localhost:5434 plus its own
#       Redis/redis-commander (ports 6379/8081 clash with Unity's, so stop Unity first).

param(
    [Parameter(Position=0)]
    [string]$Command,

    [Parameter(Position=1)]
    [string]$Service
)

$ErrorActionPreference = "Stop"
$Host.UI.RawUI.WindowTitle = "Grants Application - Local Dev Environment"

$databaseName = "UnityApplicantPortal"
$unityDbContainer = "unitydb"
$unityNetwork = "unity_local_common-network"
$appServices = @("backend", "frontend")
$projectImages = @("grants-backend", "grants-frontend", "grants-migrator")

# docker-compose.yml lives in Grants.ApplicantPortal/, not alongside this script —
# switch there so docker compose finds it, no matter where this is invoked from.
$composeDirectory = Join-Path $PSScriptRoot "Grants.ApplicantPortal"
$isSharedMode = Test-Path -PathType Leaf -LiteralPath (Join-Path $composeDirectory "docker-compose.override.yml")

$unityRepo = if ($env:UNITY_REPO) { $env:UNITY_REPO } else { Join-Path $PSScriptRoot "..\..\Unity" }
$unityGrantManagerDirectory = [System.IO.Path]::GetFullPath((Join-Path $unityRepo "applications\Unity.GrantManager"))
$unityEnvFile = Join-Path $unityGrantManagerDirectory ".env"

function Invoke-Compose {
    $composeArgs = @()
    if ($isSharedMode) {
        # Supplies UNITY_POSTGRES_PASSWORD / REDIS_PASSWORD to the override without copying secrets.
        $composeArgs += @("--env-file", $unityEnvFile)
    }
    docker compose @composeArgs @args
    if ($LASTEXITCODE -ne 0) {
        throw "docker compose $($args -join ' ') failed with exit code $LASTEXITCODE."
    }
}

function Assert-UnityDatabase {
    if (-not (Test-Path -PathType Leaf -LiteralPath $unityEnvFile)) {
        throw "Unity GrantManager .env not found at $unityEnvFile. Set `$env:UNITY_REPO to your Unity repo folder."
    }
    docker network inspect $unityNetwork 1>$null 2>$null
    if ($LASTEXITCODE -ne 0) {
        throw "Docker network '$unityNetwork' not found. Start Unity first: docker compose up -d (in $unityGrantManagerDirectory)."
    }
    $state = docker inspect --format "{{.State.Status}}" $unityDbContainer 2>$null
    if ($state -ne "running") {
        throw "Shared Postgres container '$unityDbContainer' is not running. Start Unity first: docker compose up -d (in $unityGrantManagerDirectory)."
    }
    $exists = docker exec $unityDbContainer psql -U postgres -Atc "select 1 from pg_database where datname = '$databaseName'" 2>$null
    if ($exists -ne "1") {
        Write-Warning "Database '$databaseName' does not exist in '$unityDbContainer'. Run '.\local-docker.ps1 migrate' to create and seed it."
    }
}

function Start-Services {
    if ($isSharedMode) {
        Assert-UnityDatabase
        # Only the app services: Postgres and Redis come from Unity's stack.
        Invoke-Compose up -d @appServices
    } else {
        Invoke-Compose up -d
    }
}

function Show-Services {
    Write-Host
    Write-Host "Services:" -ForegroundColor Cyan
    Write-Host "Frontend: http://localhost:4200" -ForegroundColor Yellow
    Write-Host "Backend API: http://localhost:5100" -ForegroundColor Yellow
    if ($isSharedMode) {
        Write-Host "PostgreSQL: localhost:5432 (shared Unity container '$unityDbContainer', Database: $databaseName)" -ForegroundColor Yellow
        Write-Host "Redis: localhost:6379 (shared Unity Redis)" -ForegroundColor Yellow
    } else {
        Write-Host "PostgreSQL: localhost:5434 (container grants-postgres, User: postgres, Password: localdev, Database: $databaseName)" -ForegroundColor Yellow
        Write-Host "Redis Commander: http://localhost:8081" -ForegroundColor Yellow
    }
    Write-Host
    Write-Host "Use '.\local-docker.ps1 logs' to view logs" -ForegroundColor Gray
    Write-Host "Use '.\local-docker.ps1 stop' to stop all services" -ForegroundColor Gray
}

function Show-Usage {
    $mode = if ($isSharedMode) { "shared (Unity '$unityDbContainer' on localhost:5432)" } else { "standalone (grants-postgres on localhost:5434)" }
    Write-Host "Grants Application - Development Environment Helper (Docker)" -ForegroundColor Cyan
    Write-Host "Mode: $mode, Database: $databaseName" -ForegroundColor Gray
    Write-Host
    Write-Host "Usage:"
    Write-Host "  .\local-docker.ps1 start    - Start all services"
    Write-Host "  .\local-docker.ps1 stop     - Stop all services"
    Write-Host "  .\local-docker.ps1 logs     - View logs from all services"
    Write-Host "  .\local-docker.ps1 logs SERVICE - View logs for a specific service (frontend, backend)"
    Write-Host "  .\local-docker.ps1 rebuild  - Rebuild and restart all services"
    Write-Host "  .\local-docker.ps1 migrate  - Run the DB migrator/seeder against $databaseName (shared mode)"
    Write-Host "  .\local-docker.ps1 clean    - Remove this environment's containers, volumes, networks and images"
    Write-Host
    Write-Host "For running frontend/backend directly on this machine (no Docker), use"
    Write-Host "  .\start-backend.ps1, .\start-frontend.ps1 and .\stop-local.ps1"
    Write-Host
}

Push-Location $composeDirectory
try {
switch ($Command) {
    "start" {
        Write-Host "Starting local development environment..." -ForegroundColor Green
        Start-Services
        Show-Services
    }
    "stop" {
        Write-Host "Stopping all services..." -ForegroundColor Yellow
        Invoke-Compose down
        Write-Host "All services stopped." -ForegroundColor Green
    }
    "logs" {
        if ([string]::IsNullOrEmpty($Service)) {
            Write-Host "Showing logs for all services (press Ctrl+C to exit)..." -ForegroundColor Cyan
            Invoke-Compose logs -f
        } else {
            Write-Host "Showing logs for $Service (press Ctrl+C to exit)..." -ForegroundColor Cyan
            Invoke-Compose logs -f $Service
        }
    }
    "rebuild" {
        Write-Host "Rebuilding and starting services..." -ForegroundColor Yellow
        Invoke-Compose down
        if ($isSharedMode) { Invoke-Compose build @appServices } else { Invoke-Compose build }
        Start-Services
        Write-Host "Services rebuilt and started." -ForegroundColor Green
        Show-Services
    }
    "migrate" {
        if (-not $isSharedMode) {
            throw "The migrator service is defined in docker-compose.override.yml. Copy docker-compose.override.yml.example to docker-compose.override.yml first."
        }
        Assert-UnityDatabase
        Write-Host "Running DB migrator against $databaseName in '$unityDbContainer'..." -ForegroundColor Yellow
        Invoke-Compose --profile tools run --rm --build migrator
        Write-Host "Migrations applied." -ForegroundColor Green
    }
    "clean" {
        Write-Host "Removing all containers, volumes, and networks for the project..." -ForegroundColor Yellow
        Invoke-Compose down --volumes --remove-orphans

        Write-Host "Removing project-built Docker images..." -ForegroundColor Yellow
        foreach ($image in $projectImages) {
            $imageIds = docker images --filter "reference=$image" -q
            if ($imageIds) { docker rmi $imageIds -f }
        }

        Write-Host "Environment cleaned successfully." -ForegroundColor Green
        if ($isSharedMode) {
            Write-Host "Note: $databaseName lives in Unity's '$unityDbContainer' container and was NOT removed." -ForegroundColor Gray
        }
        Write-Host "Use '.\local-docker.ps1 start' to start with fresh containers." -ForegroundColor Gray
    }
    default {
        Show-Usage
    }
}
} finally {
    Pop-Location
}
