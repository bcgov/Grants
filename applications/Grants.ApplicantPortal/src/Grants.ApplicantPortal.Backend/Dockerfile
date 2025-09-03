# Use the official .NET SDK image as the base image for building the application
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

# Set the working directory
WORKDIR /app

# Copy solution file and Directory.Packages.props for central package management
COPY Grants.ApplicantPortal.API.sln ./
COPY Directory.Packages.props ./
COPY Directory.Build.props ./
COPY global.json ./
COPY nuget.config ./
COPY GlobalSuppressions.cs ./

# Copy all project files first for better layer caching
COPY src/Grants.ApplicantPortal.API.Web/*.csproj ./src/Grants.ApplicantPortal.API.Web/
COPY src/Grants.ApplicantPortal.API.Core/*.csproj ./src/Grants.ApplicantPortal.API.Core/
COPY src/Grants.ApplicantPortal.API.Infrastructure/*.csproj ./src/Grants.ApplicantPortal.API.Infrastructure/
COPY src/Grants.ApplicantPortal.API.UseCases/*.csproj ./src/Grants.ApplicantPortal.API.UseCases/
COPY src/Grants.ApplicantPortal.API.ServiceDefaults/*.csproj ./src/Grants.ApplicantPortal.API.ServiceDefaults/

# Restore dependencies
RUN dotnet restore src/Grants.ApplicantPortal.API.Web/Grants.ApplicantPortal.API.Web.csproj

# Copy the rest of the application code
COPY src/ ./src/

# Build and publish the Web project
RUN dotnet publish src/Grants.ApplicantPortal.API.Web/Grants.ApplicantPortal.API.Web.csproj -c Release -o /out

# Use the official .NET runtime image for running the application
FROM mcr.microsoft.com/dotnet/aspnet:9.0

# Set the working directory
WORKDIR /app

# Copy the built application from the build stage
COPY --from=build /out .

# Set environment variable to listen on port 5100
ENV ASPNETCORE_URLS=http://+:5100

# Expose port 5100
EXPOSE 5100

# Start the application
ENTRYPOINT ["dotnet", "Grants.ApplicantPortal.API.Web.dll"]