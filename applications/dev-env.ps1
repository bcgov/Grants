# Grants Application - Local Development Environment PowerShell Script

param(
    [Parameter(Position=0)]
    [string]$Command,
    
    [Parameter(Position=1)]
    [string]$Service
)

$Host.UI.RawUI.WindowTitle = "Grants Application - Local Dev Environment"

# Set project name to "grants" instead of the folder name
$env:COMPOSE_PROJECT_NAME = "grants"

function Show-Usage {
    Write-Host "Grants Application - Development Environment Helper" -ForegroundColor Cyan
    Write-Host
    Write-Host "Usage:"
    Write-Host "  .\dev-env.ps1 start    - Start all services"
    Write-Host "  .\dev-env.ps1 stop     - Stop all services"
    Write-Host "  .\dev-env.ps1 logs     - View logs from all services"
    Write-Host "  .\dev-env.ps1 logs SERVICE - View logs for a specific service (frontend, backend, postgres)"
    Write-Host "  .\dev-env.ps1 rebuild  - Rebuild and restart all services"
    Write-Host
    Write-Host "Or simply use standard Docker Compose commands:"
    Write-Host "  docker-compose up -d   - Start all services in detached mode"
    Write-Host "  docker-compose down    - Stop all services"
    Write-Host "  docker-compose logs -f - View logs (follow mode)"
    Write-Host
}

switch ($Command) {
    "start" {
        Write-Host "Starting local development environment..." -ForegroundColor Green
        docker-compose up -d
        
        Write-Host
        Write-Host "Services:" -ForegroundColor Cyan
        Write-Host "Frontend: http://localhost:4000" -ForegroundColor Yellow
        Write-Host "Backend API: http://localhost:5100" -ForegroundColor Yellow
        Write-Host "PostgreSQL: localhost:5434 (User: postgres, Password: localdev, Database: GrantsDB)" -ForegroundColor Yellow
        Write-Host
        Write-Host "Use '.\dev-env.ps1 logs' to view logs" -ForegroundColor Gray
        Write-Host "Use '.\dev-env.ps1 stop' to stop all services" -ForegroundColor Gray
    }
    "stop" {
        Write-Host "Stopping all services..." -ForegroundColor Yellow
        docker-compose down
        Write-Host "All services stopped." -ForegroundColor Green
    }
    "logs" {
        if ([string]::IsNullOrEmpty($Service)) {
            Write-Host "Showing logs for all services (press Ctrl+C to exit)..." -ForegroundColor Cyan
            docker-compose logs -f
        } else {
            Write-Host "Showing logs for $Service (press Ctrl+C to exit)..." -ForegroundColor Cyan
            docker-compose logs -f $Service
        }
    }
    "rebuild" {
        Write-Host "Rebuilding and starting services..." -ForegroundColor Yellow
        docker-compose down
        docker-compose build
        docker-compose up -d
        Write-Host "Services rebuilt and started." -ForegroundColor Green
    }
    default {
        Show-Usage
    }
}