# Grants Applicant Portal Solution

This program solution allows applicants to interact with multiple grant programs through a modern web interface.

## Application Architecture

- **Frontend**: Angular 20 standalone-component SPA, served by an Express.js container
- **Backend**: ASP.NET Core 9 API (FastEndpoints + CQRS) with PostgreSQL and Redis
- **Deployment**: Docker containers with OpenShift deployment configuration
- **Authentication**: Integration with BC Government Single Sign-On

## Getting Started

New to the project? Read the orientation briefing first: [Applicant Portal, from zero](../documentation/handover/Applicant-Portal-Orientation.html). For setup and deployment specifics, see the individual component READMEs below.

## Key Components

- [Grants.ApplicantPortal](Grants.ApplicantPortal/README.md) - The main application with frontend and backend services
- [Grants.AutoUI](Grants.AutoUI/README.md) - Automated UI testing framework
