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

## 📜 MediatR Licensing Notice

This project makes use of [MediatR](https://github.com/LuckyPennySoftware/MediatR), which is dual-licensed under:

- **Reciprocal Public License 1.5 (RPL-1.5)** for community/open-source use
- **Commercial License** for for-profit/enterprise scenarios

As this software is developed as **open-source** for the **British Columbia government**, it qualifies for **free community use** under MediatR's licensing model.  

> For clarity, MediatR’s maintainer has stated that **government agencies and non-profit organizations** are eligible to use the library without commercial licensing fees.

If required for audit or compliance purposes, a **Community license key** may be obtained.  

Additional details can be found in the MediatR [GitHub discussions](https://github.com/LuckyPennySoftware/MediatR/discussions/1123) and [licensing update blog](https://www.jimmybogard.com/automapper-and-mediatr-licensing-update/).
