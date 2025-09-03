@echo off
echo Starting database migrations and seeding...
echo.
dotnet run --project src\Grants.ApplicantPortal.API.Migrations\Grants.ApplicantPortal.API.Migrations.csproj
echo.
if %ERRORLEVEL% EQU 0 (
    echo ? Migration and seeding completed successfully!
) else (
    echo ? Migration failed with error code %ERRORLEVEL%
)
echo.
pause