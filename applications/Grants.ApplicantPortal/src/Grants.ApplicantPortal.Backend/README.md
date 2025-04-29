# Backend Docker Setup

This document provides instructions for building and running the Backend application using Docker.

## Prerequisites

- Ensure Docker is installed and running on your system.
- Verify that the `appsettings.json` file contains the correct configuration for your database and other dependencies.

## Build and Run Steps

1. Navigate to the Backend directory:
   ```bash
   cd applications/Grants.ApplicantPortal/src/Grants.ApplicantPortal.Backend
   ```

2. Build the Docker image:
   ```bash
   docker build -t grants-backend .
   ```

3. Run the Docker container:
   ```bash
   docker run -p 5100:5100 grants-backend
   ```

4. Access the application:
   - The application will be available at `http://localhost:5100`.

## Notes

- Ensure the database and Redis services are running and accessible as per the connection strings in `appsettings.json`.
- Update the `Dockerfile` if additional dependencies or configurations are required.