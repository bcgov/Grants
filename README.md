# Grants Application Portal

[![Lifecycle:Stable](https://img.shields.io/badge/Lifecycle-Stable-97ca00)](https://github.com/bcgov/repomountie/blob/master/doc/lifecycle-badges.md)
The project is in a reliable state and major changes are unlikely to happen.

## Project Overview

The Grants Application Portal is a modern solution that allows the BC Government to:

- Provide a user-friendly interface for applicants to track grant approval status

### Key Features

This project provides a grants application system with the following features:

- User-friendly portal for applicants to track grant applications
- Secure authentication and authorization via BC Government Single Sign-On
- Modern web architecture with Angular frontend and .NET backend
- Highly available PostgreSQL database for secure data storage
- Containerized deployment on OpenShift infrastructure
- Security compliance with BC Government standards
- Health monitoring for all system components

## Directory Structure

    .github/                   - GitHub Actions
    applications/              - Application Root
    ├── Grants.ApplicantPortal/ - Applicant Information solution (frontend + backend)
    └── Grants.AutoUI/          - Automated User Interface testing (Cypress)
    documentation/             - Solution documentation and assets
    COMPLIANCE.yaml            - BCGov PIA/STRA compliance status
    CONTRIBUTING.md            - Contribution guidelines
    LICENSE                    - License
    SECURITY.md                - Security Policy and Reporting

## Local Development

Grants shares Unity's local stack, the same way the OpenShift `dev` and `dev2` environments share one PostgreSQL cluster:

1. Start Unity's `Unity.GrantManager` stack (`docker compose up -d`). It provides PostgreSQL (`unitydb`, `localhost:5432`), Redis and RabbitMQ. Clone the Unity repo next to this one, or set `$env:UNITY_REPO`.
2. The Grants database is `UnityApplicantPortal`, matching OpenShift. It is created and seeded by the DB migrator.

From `applications/`:

| Mode | Start | Stop | URLs |
| ---- | ----- | ---- | ---- |
| Docker | `.\local-docker.ps1 start` (first time: copy `Grants.ApplicantPortal\docker-compose.override.yml.example` to `docker-compose.override.yml`, then `.\local-docker.ps1 migrate`) | `.\local-docker.ps1 stop` | Frontend `http://localhost:4200`, API `http://localhost:5100` |
| No Docker | `.\start-backend.ps1` (runs the DB migrator, then the API), then `.\start-frontend.ps1` | `.\stop-local.ps1` | Frontend `http://localhost:4200`, API `https://localhost:7000` |

Running without Docker needs the .NET 10 SDK, Node.js 22, a trusted dev certificate (`dotnet dev-certs https --trust`) and Keycloak user-secrets (`Grants.ApplicantPortal\src\Grants.ApplicantPortal.Backend\scripts\setup-dev-secrets.ps1`). See the [orientation guide](documentation/handover/Applicant-Portal-Orientation.html) for details.

## Documentation

- [Applicant Portal, from zero](documentation/handover/Applicant-Portal-Orientation.html) — orientation to the system, its conventions, and local setup
- [Documentation index](documentation/README.md) — architecture, guides, integration specs, ADRs
- [Application Readme](applications/README.md)
