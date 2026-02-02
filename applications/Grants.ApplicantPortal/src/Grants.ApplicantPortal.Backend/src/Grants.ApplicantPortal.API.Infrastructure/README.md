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
```

## Caching Configuration

The application supports both **in-memory caching** and **Redis distributed caching**. The caching provider is automatically selected based on the presence of the Redis connection string.

### Automatic Cache Provider Selection

- **If Redis connection string is present**: The application uses Redis for distributed caching
- **If Redis connection string is not present**: The application falls back to in-memory caching

### Redis Configuration (Optional)

To enable Redis caching, add a `Redis` connection string to your `appsettings.json` or `appsettings.Development.json`:

```json
"ConnectionStrings": {
  "DefaultConnection": "Host=localhost;Database=ApplicantPortal;Username=postgres;Password=admin;Port=5432",
  "Redis": "localhost:6379,password=password,defaultDatabase=1,abortConnect=false"
}
```

#### Redis Connection String Examples

**Basic Redis Connection (no password):**
```json
"Redis": "localhost:6379,defaultDatabase=1,abortConnect=false"
```

**Redis with Authentication:**
```json
"Redis": "localhost:6379,password=yourpassword,defaultDatabase=1,abortConnect=false"
```

**Redis Sentinel Configuration:**
```json
"Redis": "sentinel1:26379,sentinel2:26379,sentinel3:26379,serviceName=mymaster,defaultDatabase=1,abortConnect=false"
```

**Redis Sentinel with Authentication:**
```json
"Redis": "sentinel1:26379,sentinel2:26379,sentinel3:26379,serviceName=mymaster,defaultDatabase=1,password=yourpassword,abortConnect=false"
```

### Using Docker for Redis (Optional)

You can run Redis using Docker:

```bash
# Redis without password
docker run --name redis-dev -d -p 6379:6379 redis:7

# Redis with password
docker run --name redis-dev -d -p 6379:6379 redis:7 redis-server --requirepass yourpassword
```

### Profile Cache Settings

The profile caching behavior is configured in the `ProfileCache` section:

```json
"ProfileCache": {
  "CacheKeyPrefix": "profile:",
  "CacheExpiryMinutes": 60,
  "SlidingExpiryMinutes": 15
}
```

- **CacheKeyPrefix**: Prefix for all profile cache keys
- **CacheExpiryMinutes**: Absolute expiration time in minutes
- **SlidingExpiryMinutes**: Sliding expiration time in minutes (cache is extended if accessed)


