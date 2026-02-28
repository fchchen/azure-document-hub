#!/bin/bash
set -e

# Configuration
RESOURCE_GROUP="${RESOURCE_GROUP:-rg-document-hub}"
LOCATION="${LOCATION:-canadacentral}"
ENVIRONMENT="${ENVIRONMENT:-dev}"
COSMOS_ACCOUNT_NAME="${COSMOS_ACCOUNT_NAME:-codeagent-cosmos-bnb5kltohjuh4}"

echo "=========================================="
echo "Document Hub Infrastructure Deployment"
echo "=========================================="
echo "Resource Group:      $RESOURCE_GROUP"
echo "Location:            $LOCATION"
echo "Environment:         $ENVIRONMENT"
echo "Cosmos Account:      $COSMOS_ACCOUNT_NAME"
echo "=========================================="

# Check if Azure CLI is installed
if ! command -v az &> /dev/null; then
    echo "Error: Azure CLI is not installed. Please install it first."
    exit 1
fi

# Check if logged in
if ! az account show &> /dev/null; then
    echo "Please log in to Azure..."
    az login
fi

# Create resource group if it doesn't exist
echo "Creating resource group..."
az group create \
    --name "$RESOURCE_GROUP" \
    --location "$LOCATION" \
    --output none

# Deploy infrastructure
echo "Deploying infrastructure..."
DEPLOYMENT_OUTPUT=$(az deployment group create \
    --resource-group "$RESOURCE_GROUP" \
    --template-file main.bicep \
    --parameters \
        location="$LOCATION" \
        environment="$ENVIRONMENT" \
        cosmosAccountName="$COSMOS_ACCOUNT_NAME" \
    --query 'properties.outputs' \
    --output json)

# Extract outputs
API_URL=$(echo "$DEPLOYMENT_OUTPUT" | jq -r '.apiAppUrl.value')
FUNCTION_URL=$(echo "$DEPLOYMENT_OUTPUT" | jq -r '.functionAppUrl.value')
STATIC_WEB_URL=$(echo "$DEPLOYMENT_OUTPUT" | jq -r '.staticWebAppUrl.value')
STORAGE_ACCOUNT=$(echo "$DEPLOYMENT_OUTPUT" | jq -r '.storageAccountName.value')

echo ""
echo "=========================================="
echo "Deployment Complete!"
echo "=========================================="
echo "API URL:         $API_URL"
echo "Function App:    $FUNCTION_URL"
echo "Frontend URL:    $STATIC_WEB_URL"
echo "Storage Account: $STORAGE_ACCOUNT"
echo "=========================================="

# Save outputs
cat > .env.azure << EOF
AZURE_RESOURCE_GROUP=$RESOURCE_GROUP
API_URL=$API_URL
FUNCTION_APP_URL=$FUNCTION_URL
STATIC_WEB_URL=$STATIC_WEB_URL
STORAGE_ACCOUNT=$STORAGE_ACCOUNT
COSMOS_ACCOUNT_NAME=$COSMOS_ACCOUNT_NAME
EOF

echo "Environment variables saved to .env.azure"
