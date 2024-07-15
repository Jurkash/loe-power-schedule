#!/bin/bash

# Ensure that the script parameter is provided
if [ $# -eq 0 ]; then
  echo "No arguments provided. Please provide the lps value as a parameter."
  echo "Usage: $0 <lps>"
  exit 1
fi

# Get the lps value from the script parameter
lps=$1

# Build the Docker image
docker build \
  -t loepowerschedule.azurecr.io/loepowerschedule:$lps \
  -f ./Dockerfile \
  ../

# Check if the Docker build was successful
if [ $? -ne 0 ]; then
  echo "Docker build failed."
  exit 1
fi

# Push the Docker image to Azure Container Registry
docker push loepowerschedule.azurecr.io/loepowerschedule:$lps

# Check if the Docker push was successful
if [ $? -ne 0 ]; then
  echo "Docker push failed."
  exit 1
fi

# Update the Azure Container App
az containerapp update \
  --resource-group Loe-Sandbox \
  --name loe-power-schedule-app \
  --image loepowerschedule.azurecr.io/loepowerschedule:$lps

# Check if the Azure Container App update was successful
if [ $? -ne 0 ]; then
  echo "Azure Container App update failed."
  exit 1
fi

echo "Deployment completed successfully."
``
