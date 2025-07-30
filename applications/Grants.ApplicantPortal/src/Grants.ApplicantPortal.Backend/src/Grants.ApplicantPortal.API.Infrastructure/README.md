## Infrastructure Project

In Clean Architecture, Infrastructure concerns are kept separate from the core business rules (or domain model in DDD).

The only project that should have code concerned with EF, Files, Email, Web Services, Azure/AWS/GCP, etc is Infrastructure.

Infrastructure should depend on Core (and, optionally, Use Cases) where abstractions (interfaces) exist.

Infrastructure classes implement interfaces found in the Core (Use Cases) project(s).

These implementations are wired up at startup using DI. In this case using `Microsoft.Extensions.DependencyInjection` and extension methods defined in each project.

Need help? Check out the sample here:
https://github.com/ardalis/CleanArchitecture/tree/main/sample

Still need help?
Contact us at https://nimblepros.com


## Database Configuration

This project now uses **PostgreSQL** as the database provider. Make sure you have PostgreSQL installed and running before executing migrations or running the application.

### Connection String Configuration

The connection string is configured in `appsettings.json` and `appsettings.Development.json` using the `DefaultConnection` key:

- **Production**: `DefaultConnection` in `appsettings.json`
- **Development**: `DefaultConnection` in `appsettings.Development.json`

Example connection string format:
```
Host=localhost;Database=applicant_portal;Username=postgres;Password=admin;Port=5432
```

## Running Migrations

When running from the solution folder, you can run the following command to add a new migration:

```bash
dotnet ef migrations add <MigrationName> --startup-project src\Grants.ApplicantPortal.API.Web --context AppDbContext --project src\Grants.ApplicantPortal.API.Infrastructure --output-dir Data/Migrations
```

To update the database, run the following command:
```bash
dotnet ef database update --startup-project src\Grants.ApplicantPortal.API.Web --context AppDbContext --project src\Grants.ApplicantPortal.API.Infrastructure
```

## Setting Up PostgreSQL

Before running the application, ensure PostgreSQL is installed and configured:

1. Install PostgreSQL on your development machine
2. Create a database (e.g., `applicant_portal` for production, `grants_applicant_portal_dev` for development)
3. Update the connection string in `appsettings.json` and `appsettings.Development.json` with your PostgreSQL credentials
4. Run the migrations to create the database schema

### Using Docker for PostgreSQL (Optional)

You can also run PostgreSQL using Docker:

```bash
docker run --name postgres-dev -e POSTGRES_PASSWORD=admin -d -p 5432:5432 postgres:15
docker exec -it postgres-dev createdb -U postgres applicant_portal

