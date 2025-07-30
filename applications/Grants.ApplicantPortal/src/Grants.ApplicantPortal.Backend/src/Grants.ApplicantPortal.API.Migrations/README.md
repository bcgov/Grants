# Database Migrations Project

This project is responsible for applying database migrations and seeding initial data for the Grants Applicant Portal application.

## Prerequisites

- PostgreSQL server running
- .NET 9.0 SDK installed
- Docker (optional, for containerized deployment)

## Configuration

Update the connection strings in:
- `appsettings.json` for production
- `appsettings.Development.json` for development

Default connection string format:
```
Host=localhost;Database=ApplicantPortal;Username=postgres;Password=admin;Port=5432
```

## Running as Startup Project

### Option 1: Using Visual Studio
1. Right-click on `Grants.ApplicantPortal.API.Migrations` project in Solution Explorer
2. Select "Set as Startup Project"
3. Press F5 or click "Start Debugging"

### Option 2: Using Command Line
```bash
cd src\Grants.ApplicantPortal.API.Migrations
dotnet run
```

### Option 3: Using dotnet run with specific project
```bash
dotnet run --project src\Grants.ApplicantPortal.API.Migrations\Grants.ApplicantPortal.API.Migrations.csproj
```

### Option 4: Using the provided batch script
```bash
run-migrations.bat
```

## Running as Docker Container

### Build Docker Image
```bash
docker build -f src\Grants.ApplicantPortal.API.Migrations\Dockerfile -t grants-migrations .
```

### Run Container (One-time execution)
```bash
docker run --rm --name grants-migrations \
  -e ConnectionStrings__DefaultConnection="Host=host.docker.internal;Database=ApplicantPortal;Username=postgres;Password=admin;Port=5432" \
  grants-migrations
```

### Run with PostgreSQL Container
If you're running PostgreSQL in Docker:
```bash
# Start PostgreSQL
docker run --name postgres-grants -e POSTGRES_PASSWORD=admin -e POSTGRES_DB=ApplicantPortal -d -p 5432:5432 postgres:15

# Run migrations
docker run --rm --name grants-migrations \
  --link postgres-grants:postgres \
  -e ConnectionStrings__DefaultConnection="Host=postgres;Database=ApplicantPortal;Username=postgres;Password=admin;Port=5432" \
  grants-migrations
```

## What This Project Does

1. **Database Migrations**: Applies all pending Entity Framework migrations to bring the database schema up to date
2. **Data Seeding**: Populates the database with initial test data including:
   - Sample contributors
   - Sample profiles

## Environment Variables

You can override configuration using environment variables:

- `ConnectionStrings__DefaultConnection`: Database connection string
- `ASPNETCORE_ENVIRONMENT`: Environment (Development, Production, etc.)

## Exit Codes

- `0`: Success - migrations and seeding completed successfully
- `1`: Failure - an error occurred during the process

## Logging

The application uses Serilog for logging. Logs are written to the console and include:
- Migration progress
- Seeding operations
- Error details

## Troubleshooting

1. **Connection Issues**: Ensure PostgreSQL is running and connection string is correct
2. **Permission Issues**: Ensure the database user has sufficient privileges to create/modify schema
3. **Container Issues**: When running in Docker, use `host.docker.internal` to connect to localhost services
4. **ConfigurationManager Issues**: This has been resolved - the project no longer requires ConfigurationManager casting

## Recent Fixes

- ? Fixed `ConfigurationManager` casting issue that was causing startup failures
- ? Simplified service configuration to only register required dependencies for migrations
- ? Improved error handling and logging

## Development

For development, you can modify:
- `SeedData.cs` in the Infrastructure project to add/modify seed data
- Connection strings in `appsettings.Development.json`
- Logging configuration in appsettings files