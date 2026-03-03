# Unity Mock API

This is a standalone HTTP API that provides mock data endpoints for testing the Unity plugin during development.

## Overview

The Unity Mock API simulates the Unity system endpoints that the Unity plugin calls to retrieve profile data. Instead of having mock data generation logic within the plugin itself, all mock data is now served by this dedicated API.

## Features

- **Swagger UI**: Interactive API documentation
- **Health Check**: Monitor API status
- **All Unity Endpoints**: Complete coverage of Unity plugin data types
- **CORS Enabled**: Cross-origin requests supported for development

## Getting Started

### Prerequisites

- .NET 9 SDK
- PowerShell (cross-platform)

### Starting the API

#### Option 1: Using PowerShell Script (Recommended)

```powershell
# Navigate to the project root
cd C:\Repos\Grants\applications\Grants.ApplicantPortal\src\Grants.ApplicantPortal.Backend

# Run the start script
.\scripts\Start-UnityMockAPI.ps1
```

**Script Options:**
```powershell
# Start on custom port
.\scripts\Start-UnityMockAPI.ps1 -Port 8080

# Start in Production mode
.\scripts\Start-UnityMockAPI.ps1 -Environment Production

# Combine options
.\scripts\Start-UnityMockAPI.ps1 -Port 8080 -Environment Production
```

#### Option 2: Manual dotnet run

```bash
# Navigate to the Unity Mock API project
cd src/Grants.ApplicantPortal.API.Unity.MockAPI

# Build the project
dotnet build

# Run the API
dotnet run
```

### Accessing the API

Once started, the Unity Mock API will be available at:

- **Base URL**: http://localhost:5555
- **Swagger UI**: http://localhost:5555/swagger
- **Health Check**: http://localhost:5555/health

## API Endpoints

### Profile Endpoints

| Endpoint | Method | Parameters | Description |
|----------|--------|------------|-------------|
| `/api/v1/profiles/{profileId}` | GET | `?provider=DGP\|ABC` | Get basic profile information |
| `/api/v1/profiles/{profileId}/employment` | GET | `?provider=DGP\|ABC` | Get employment information |
| `/api/v1/profiles/{profileId}/security` | GET | `?provider=DGP\|ABC` | Get security clearance data |
| `/api/v1/profiles/{profileId}/contacts` | GET | `?provider=DGP\|ABC` | Get contact information |
| `/api/v1/profiles/{profileId}/addresses` | GET | `?provider=DGP\|ABC` | Get address information |
| `/api/v1/profiles/{profileId}/organization` | GET | `?provider=DGP\|ABC` | Get organization information |
| `/api/v1/profiles/{profileId}/submissions` | GET | `?provider=DGP\|ABC` | Get submission data |
| `/api/v1/profiles/{profileId}/payments` | GET | `?provider=DGP\|ABC` | Get payment information |
| `/api/v1/profiles/{profileId}/data` | GET | `?provider=DGP\|ABC` | Get default/fallback data |

### Utility Endpoints

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/health` | GET | Health check endpoint |

## Configuration

### Main Application Configuration

Update your `appsettings.Development.json` to point the Unity plugin to the mock API:

```json
{
  "Plugins": {
    "UNITY": {
      "Configuration": {
        "BaseUrl": "http://localhost:5555"
      }
    }
  }
}
```

### Mock API Configuration

The mock API configuration is in `src/Grants.ApplicantPortal.API.Unity.MockAPI/appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "Urls": "http://localhost:5555"
}
```

## Sample Data

The mock API returns structured JSON data that matches the expected Unity system response format. Each response includes:

- **Metadata**: ProfileId, Provider, Key, Source, PopulatedAt, etc.
- **Data**: The actual profile data based on the endpoint called
- **Mock Indicators**: `IsMockData: true` and `Source: "Unity Mock API"`

### Example Response

```json
{
  "profileId": "123e4567-e89b-12d3-a456-426614174000",
  "provider": "DGP",
  "key": "PROFILE",
  "source": "Unity Mock API",
  "populatedAt": "2024-01-15T10:30:00Z",
  "populatedBy": "UNITY",
  "isMockData": true,
  "data": {
    "personalInfo": {
      "firstName": "John",
      "lastName": "Doe",
      "email": "john.doe@unity.gov",
      "phone": "+1-555-0123",
      "employeeId": "UNI-12345"
    }
  }
}
```

**Supported Providers**: DGP, ABC

**Example Requests**:
```bash
# Get DGP profile data
curl "http://localhost:5555/api/v1/profiles/123e4567-e89b-12d3-a456-426614174000?provider=DGP"

# Get ABC addresses data  
curl "http://localhost:5555/api/v1/profiles/123e4567-e89b-12d3-a456-426614174000/addresses?provider=ABC"
```

## Development Workflow

1. **Start Unity Mock API**: Run `.\scripts\Start-UnityMockAPI.ps1`
2. **Start Main Application**: Run your main Grants Portal API
3. **Test Integration**: The Unity plugin will now fetch data from the mock API
4. **View Logs**: Monitor both APIs to see the data flow
5. **Modify Mock Data**: Edit the Unity Mock API endpoints as needed

## Troubleshooting

### Port Already in Use

If port 5555 is already in use:

```powershell
# Use a different port
.\scripts\Start-UnityMockAPI.ps1 -Port 5556

# Update your appsettings.Development.json accordingly
```

### Connection Issues

1. Ensure the Unity Mock API is running and accessible
2. Check firewall settings
3. Verify the BaseUrl in your plugin configuration matches the mock API URL
4. Check the API health endpoint: http://localhost:5555/health

### Build Issues

```bash
# Clean and rebuild
cd src/Grants.ApplicantPortal.API.Unity.MockAPI
dotnet clean
dotnet restore
dotnet build
```

## Benefits

- **Separation of Concerns**: Mock data is external to the plugin logic
- **Realistic Testing**: HTTP calls simulate real Unity system integration
- **Flexibility**: Easy to modify mock data without rebuilding the main application
- **Documentation**: Swagger UI provides clear API documentation
- **Debugging**: Easy to monitor HTTP traffic and responses

## Next Steps

- **Custom Test Scenarios**: Modify endpoints to test error conditions, timeouts, etc.
- **Dynamic Data**: Add support for generating different data based on ProfileId
- **Database Integration**: Optionally persist mock data for consistent testing
- **Docker Support**: Containerize the mock API for easier deployment