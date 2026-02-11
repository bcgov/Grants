@echo off
setlocal

REM Start Unity Mock API for testing the Unity plugin
REM This batch file provides a simple way to start the mock API on Windows

set DEFAULT_PORT=5555
set PORT=%1
if "%PORT%"=="" set PORT=%DEFAULT_PORT%

echo.
echo ===========================================
echo   Unity Mock API Starter
echo ===========================================
echo.

REM Get the directory where this script is located
set SCRIPT_DIR=%~dp0
set PROJECT_ROOT=%SCRIPT_DIR%..
set UNITY_MOCK_API_PATH=%PROJECT_ROOT%\src\Grants.ApplicantPortal.API.Unity.MockAPI

echo Checking if Unity Mock API project exists...
if not exist "%UNITY_MOCK_API_PATH%" (
    echo ERROR: Unity Mock API project not found at:
    echo %UNITY_MOCK_API_PATH%
    echo.
    echo Please ensure the Unity Mock API project has been created.
    pause
    exit /b 1
)

echo Project found: %UNITY_MOCK_API_PATH%
echo.

REM Check if dotnet is available
echo Checking for .NET SDK...
dotnet --version >nul 2>&1
if errorlevel 1 (
    echo ERROR: .NET SDK not found. Please install .NET 9 SDK.
    pause
    exit /b 1
)

for /f "tokens=*" %%v in ('dotnet --version') do set DOTNET_VERSION=%%v
echo Found .NET SDK version: %DOTNET_VERSION%
echo.

echo Starting Unity Mock API...
echo Project Path: %UNITY_MOCK_API_PATH%
echo Port: %PORT%
echo Environment: Development
echo.

REM Set environment variables
set ASPNETCORE_ENVIRONMENT=Development
set ASPNETCORE_URLS=http://localhost:%PORT%

REM Change to the project directory
cd /d "%UNITY_MOCK_API_PATH%"

echo Building Unity Mock API...
dotnet build --configuration Release
if errorlevel 1 (
    echo ERROR: Build failed!
    pause
    exit /b 1
)

echo Build successful!
echo.
echo ============================================
echo   Unity Mock API is starting...
echo ============================================
echo.
echo API will be available at:
echo   * API Base URL: http://localhost:%PORT%
echo   * Swagger UI: http://localhost:%PORT%/swagger
echo   * Health Check: http://localhost:%PORT%/health
echo.
echo Available Endpoints:
echo   * GET /api/app/applicant-profiles/tenants
echo   * GET /api/profiles/{profileId}
echo   * GET /api/profiles/{profileId}/employment
echo   * GET /api/profiles/{profileId}/security
echo   * GET /api/profiles/{profileId}/contacts
echo   * GET /api/profiles/{profileId}/addresses
echo   * GET /api/profiles/{profileId}/organization
echo   * GET /api/profiles/{profileId}/data
echo.
echo IMPORTANT: Update your appsettings.Development.json!
echo Set Plugins.UNITY.Configuration.BaseUrl to 'http://localhost:%PORT%'
echo.
echo Press Ctrl+C to stop the Unity Mock API
echo.

REM Start the application
dotnet run --configuration Release --no-build

REM Return to original directory
cd /d "%SCRIPT_DIR%"