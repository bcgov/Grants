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
- Highly availible PostgreSQL database for secure data storage
- Containerized deployment on OpenShift infrastructure
- Security compliance with BC Government standards
- Health monitoring for all system components

## Directory Structure

    .github                    - GitHub Actions
    applications/              - Application Root
    ├── Grants.ApplicantPortal/ - Applicant Information solution
    ├── Grants.AutoUI/          - Automated User Interface testing
    ├── Grants.Tools/           - DevOps tools
    database/                  - Database configuration files
    documentation/             - Solution documentation and assets
    openshift/                 - OpenShift deployment files
    COMPLIANCE.yaml            - BCGov PIA/STRA compliance status
    CONTRIBUTING.md            - How to contribute
    LICENSE                    - License
    SECURITY.md                - Security Policy and Reporting

## Documentation

- [Application Readme](applications/README.md)
