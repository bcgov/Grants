# Grants Application Portal

[![Lifecycle:Experimental](https://img.shields.io/badge/Lifecycle-Experimental-339999)](https://github.com/bcgov/repomountie/blob/master/doc/lifecycle-badges.md)
The project is in the very early stages of development. The codebase will be changing frequently.

## Project Overview

The Grants Application Portal is a modern solution that allows the BC Government to:

- Provide a user-friendly interface for applicants to track grant approval status

### Key Features

This project provides a grants application system with the following features:

- User-friendly portal for applicants to track grant applications
- Secure authentication and authorization BC Government Single Sign-On
- Modern web architecture with Angular frontend and .NET backend
- Containerized deployment for cloud infrastructure
- Highly available PostgreSQL database for secure data storage
- Containerized deployment on OpenShift infrastructure
- Security compliance with BC Government standards
- Health monitoring for all system components

## Directory Structure

    .github                    - GitHub Actions
    applications/              - Application Root
    ├── Grants.ApplicantPortal/ - Applicant Information solution (frontend + backend)
    ├── Grants.AutoUI/          - Automated User Interface testing (Cypress)
    └── dev-env.ps1             - Local docker-compose helper
    documentation/             - Solution documentation and assets
    COMPLIANCE.yaml            - BCGov PIA/STRA compliance status
    LICENSE                    - License
    SECURITY.md                - Security Policy and Reporting

## Documentation

- **New to the project? [Applicant Portal, from zero](documentation/handover/Applicant-Portal-Orientation.html)** — a single read-through orientation to the system, its conventions, and local setup
- [Documentation index](documentation/README.md) — architecture, guides, integration specs, ADRs
- [Application Readme](applications/README.md)
