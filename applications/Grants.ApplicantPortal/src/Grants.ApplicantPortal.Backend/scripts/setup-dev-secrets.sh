#!/bin/bash

# Quick setup script for developer secrets
# Run this from the repository root directory

echo "Setting up Grants Applicant Portal API secrets..."

# Navigate to the Web project directory
WEB_PROJECT_PATH="src/Grants.ApplicantPortal.API.Web"

if [ ! -d "$WEB_PROJECT_PATH" ]; then
    echo "? Web project directory not found: $WEB_PROJECT_PATH"
    echo "   Make sure you're running this script from the repository root directory."
    exit 1
fi

cd "$WEB_PROJECT_PATH" || exit 1

# Initialize user secrets if not already done
echo "Initializing user secrets..."
dotnet user-secrets init

# Get secrets from user input
echo ""
echo "Please provide your Keycloak configuration:"
echo ""

read -p "Client ID (Resource): " CLIENT_ID
read -s -p "Client Secret: " CLIENT_SECRET
echo ""
read -p "Auth Server URL (e.g., https://dev.loginproxy.gov.bc.ca/auth): " AUTH_SERVER_URL
read -p "Realm (e.g., standard): " REALM

# Set Keycloak configuration
echo ""
echo "Setting Keycloak configuration..."

dotnet user-secrets set "Keycloak:Resource" "$CLIENT_ID"
dotnet user-secrets set "Keycloak:Credentials:Secret" "$CLIENT_SECRET"
dotnet user-secrets set "Keycloak:AuthServerUrl" "$AUTH_SERVER_URL"
dotnet user-secrets set "Keycloak:Realm" "$REALM"

# Clear variables
unset CLIENT_SECRET

echo ""
echo "? Secrets configured successfully!"
echo ""
echo "Configured secrets:"
dotnet user-secrets list

echo ""
echo "? Setup complete! You can now:"
echo "   1. Run the application: dotnet run"
echo "   2. Use token automation scripts: ./scripts/Get-KeycloakTokenSimple.ps1"