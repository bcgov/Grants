# Use the official .NET 9 runtime image as a base image
FROM mcr.microsoft.com/dotnet/runtime:9.0 AS base
WORKDIR /app

# Use the SDK image to build the application
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project files
COPY ["src/Grants.ApplicantPortal.API.Migrations/Grants.ApplicantPortal.API.Migrations.csproj", "src/Grants.ApplicantPortal.API.Migrations/"]
COPY ["src/Grants.ApplicantPortal.API.Infrastructure/Grants.ApplicantPortal.API.Infrastructure.csproj", "src/Grants.ApplicantPortal.API.Infrastructure/"]
COPY ["src/Grants.ApplicantPortal.API.Core/Grants.ApplicantPortal.API.Core.csproj", "src/Grants.ApplicantPortal.API.Core/"]

# Copy Directory.Build.props if it exists (for central package management)
COPY ["Directory.Build.props", "./"]
COPY ["Directory.Packages.props", "./"]

# Restore dependencies
RUN dotnet restore "src/Grants.ApplicantPortal.API.Migrations/Grants.ApplicantPortal.API.Migrations.csproj"

# Copy all source code
COPY . .

# Build the application
WORKDIR "/src/src/Grants.ApplicantPortal.API.Migrations"
RUN dotnet build "Grants.ApplicantPortal.API.Migrations.csproj" -c Release -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish "Grants.ApplicantPortal.API.Migrations.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final stage - runtime image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Set environment variables
ENV ASPNETCORE_ENVIRONMENT=Production

# Expose the application (not needed for migrations, but good practice)
EXPOSE 80

# Define the entry point for the application
ENTRYPOINT ["dotnet", "Grants.ApplicantPortal.API.Migrations.dll"]