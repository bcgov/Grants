# Grants Applicant Portal SonarCloud Guide

This document provides a guide for the Grants Applicant Portal SonarCloud integration, including setup, permissions, IDE integration, and CI/CD workflows.

---

## Table of Contents

1. [Overview](#overview)
2. [Initial Setup & Login](#initial-setup--login)
3. [Project Configuration](#project-configuration)
4. [IDE Integration](#ide-integration)
5. [CI/CD Integration](#cicd-integration)
6. [Branch & Pull Request Analysis](#branch--pull-request-analysis)
7. [Support Contacts](#support-contacts)

---

## Overview

**Grants Applicant Portal SonarCloud Project Details:**

- **Organization:** `bcgov-sonarcloud`
- **Project Key:** `bcgov_Grants`
- **Project Name:** Grants
- **URL:** https://sonarcloud.io/project/overview?id=bcgov_Grants

**Technology Stack:**

- **Frontend:** Angular 20+ with Server-Side Rendering
- **Backend:** .NET 9.0 with Clean Architecture
- **Database:** PostgreSQL 17 with Entity Framework Core 7.0
- **Testing:** Karma/Jasmine (Frontend), xUnit (Backend), Cypress (E2E)
- **Build System:** Angular CLI with Vite, .NET CLI

---

## Initial Setup & Login

### First-Time SonarCloud Access

1. **Navigate to SonarCloud:** https://sonarcloud.io

2. **GitHub SSO Login:**
   - Click "Log in with GitHub"
   - Authorize SonarCloud access to your GitHub account
   - Accept organization invitation for `bcgov-sonarcloud`

3. **Join Grants Project:**
   - Navigate to: https://sonarcloud.io/project/overview?id=bcgov_Grants
   - Request access if not automatically granted

### Required Permissions After First Login

Once logged in, request the following permissions from the SonarCloud Grants Project administrators:

#### Core Permissions:

- **Users and Groups** - View team members and manage group memberships
- **Administer Issues** - Triage, assign, and resolve code issues
- **Administer Security Hotspots** - Review and manage security vulnerabilities
- **Administer Architecture** - Configure architecture rules and design constraints
- **Execute Analysis** - Trigger manual scans and view analysis results

#### Permission Request Process:

1. Contact SonarCloud Grants Project administrators
2. Provide your GitHub username

---

## Project Configuration

### Current SonarCloud Settings

**Source Code Paths:**

```properties
sonar.sources=src
sonar.tests=tests
```

**Language Configuration:**

- .NET 9.0
- TypeScript/JavaScript (Angular 20+)
- HTML/CSS/SCSS
- Java 17 (for SonarCloud scanner)

**Quality Gate:**

- **Coverage Requirement:** Disabled (`sonar.coverage.exclusions=**/*`)
- **Quality Gate Wait:** Enabled for CI feedback
- **Duplicated Lines:** Excluded for generated files

**Key Exclusions:**

```properties
# Build artifacts and dependencies
**/bin/**, **/obj/**, **/node_modules/**, **/dist/**

# Frontend build outputs
**/wwwroot/**, **/public/**

# EF Migrations
src/Grants.ApplicantPortal.Backend/src/Grants.ApplicantPortal.API.Infrastructure/Data/Migrations/**

# Generated files
**/*.Designer.cs, **/*.generated.ts
```

---

## IDE Integration

### VS Code Integration

#### 1. Install SonarLint Extension

```bash
# Via VS Code marketplace
code --install-extension SonarSource.sonarlint-vscode
```

#### 2. Configure SonarCloud Connection

**Settings (Ctrl+,):**

```json
{
  "sonarlint.connectedMode.project": {
    "connectionId": "bcgov-sonarcloud",
    "projectKey": "bcgov_Grants"
  },
  "sonarlint.connectedMode.connections.sonarcloud": [
    {
      "connectionId": "bcgov-sonarcloud",
      "organizationKey": "bcgov-sonarcloud",
      "token": "YOUR_SONARCLOUD_TOKEN"
    }
  ]
}
```

#### 3. Generate Personal Token

1. Go to: https://sonarcloud.io/account/security/
2. Generate new token with name: `Grants-SonarLint`
3. Copy token and paste in VS Code settings

#### 4. Set as a Favorite Project

1. Navigate to Grants project in SonarCloud
2. Click ⭐ "Add to favorites"
3. Access via SonarCloud dashboard favorites section

---

## CI/CD Integration

### Current GitHub Actions Setup

**Workflow File:** `.github/workflows/sonarsource-scan.yml`

#### Trigger Strategy: CI-Based Analysis

**Automatic Triggers:**

```yaml
on:
  push:
    branches: [dev, test, main]
  pull_request:
    types: [opened, synchronize, reopened]
  workflow_dispatch:
```

**Automated GitHub Analysis:**

- ✅ **CI-Based:** Build integration, PR decoration

#### Analysis Steps

1. **Environment Setup:**
   - Java 17 (SonarCloud scanner)
   - .NET 9.0 SDK
   - Node.js 20 (for Angular build)
   - dotnet-sonarscanner tool

2. **Build Process:**
   - Install Node.js dependencies: `npm ci`
   - Build Angular frontend: `npm run build:dev`
   - Restore .NET dependencies: `dotnet restore Grants.sln`
   - Build .NET solution: `dotnet build Grants.sln --no-restore`

3. **SonarCloud Scan:**
   - Uses `SonarSource/sonarqube-scan-action@v7`
   - Requires `SONAR_TOKEN` secret
   - Automatic PR decoration enabled

#### Required GitHub Secrets

- **`SONAR_TOKEN`:** SonarCloud project token
- **`GITHUB_TOKEN`:** Automatic GitHub token for PR comments

#### Required GitHub Environment Variables

- **`GRANTS_BUILD_VERSION`:** Project version set at runtime (fallback: `1.0.0`)

---

## Branch & Pull Request Analysis

### Branch Strategy Support

**Analyzed Branches:**

- `main` - Production releases
- `test` - Pre-production testing
- `dev` - Development integration

**Pull Request Analysis:**

- **Decoration:** Automatic comments on PRs with quality gate status
- **New Code Detection:** Focuses on changed lines in PR
- **Quality Gate:** Must pass for merge approval

### Quality Metrics Tracked

**Code Quality:**

- Bugs
- Vulnerabilities  
- Security Hotspots
- Code Smells
- Technical Debt

**Code Coverage:**

- **Status:** Currently disabled (`sonar.coverage.exclusions=**/*`)
- **Reason:** Simplified maintenance, focuses on other quality metrics

**Duplication:**

- Excludes generated files, build outputs, and dependencies
- Tracks duplicated code blocks in TypeScript and C# code

---

Check SonarCloud project → Quality Gates → View conditions

## Support Contacts

**SonarCloud Administration:**

- Repository Administrators
- DevOps Team Lead

**Technical Issues:**

- [SonarSource Documentation](https://docs.sonarcloud.io/)
- [GitHub Issues](https://github.com/bcgov/grants/issues)