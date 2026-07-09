# Contributing

## Overview
This repository contains the Grants Applicant Portal applications and supporting test suites.

## Testing Standards

### Cypress E2E Requirements
- End-to-end specs must validate real user-visible behavior in the target environment.
- Environment-specific branches in a spec are acceptable when production safety requires narrower assertions than lower environments.
- For the BC Services Card login flow spec, payments data presence in non-production environments is a required assertion. Do not weaken the assertion to allow empty payment results in `dev`, `dev2`, `test`, or `uat`.
- Production flows must avoid assertions or actions that depend on creating, mutating, or requiring payment data when that would be unsafe for production validation.

## Code Changes
- Keep changes small and focused.
- Preserve existing project conventions and selector usage in AutoUI tests.
- Prefer explicit assertions over broad waits or silent fallbacks when validating core business behavior.