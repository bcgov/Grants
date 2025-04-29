# Frontend Docker Setup

This document provides instructions for building and running the Frontend application using Docker.

## Prerequisites

- Ensure Docker is installed and running on your system.

## Build and Run Steps

1. Navigate to the Frontend directory:
   ```bash
   cd applications/Grants.ApplicantPortal/src/Grants.ApplicantPortal.Frontend
   ```

2. Build the Docker image:
   ```bash
   docker build -t grants-frontend .
   ```

3. Run the Docker container:
   ```bash
   docker run -p 8080:80 grants-frontend
   ```

4. Access the application:
   - The application will be available at `http://localhost:8080/`.

## Notes

- The `Dockerfile` uses a multi-stage build to first compile the Angular application and then serve it using Nginx.
- Update the `Dockerfile` if additional dependencies or configurations are required.
