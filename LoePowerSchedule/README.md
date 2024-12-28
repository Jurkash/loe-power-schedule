# LOE Power Schedule
This project is created to scrape loe for grid power off schedules and wrap that into user friendly API interface

## Deployment

0. Login into azure and azure container registry
```bash
az login
```
```bash
az acr login --name loepowerschedule
```

1. Navigate to project folder (not solution) and Build docker container 
```bash
docker build \
  -t loepowerschedule.azurecr.io/loepowerschedule:latest \
  -f ./Dockerfile \
  ../
```

2. Push container to registry
```bash
docker push loepowerschedule.azurecr.io/loepowerschedule:latest
```

3. [Optional] [Create a container app](https://learn.microsoft.com/en-us/azure/container-apps/get-started?tabs=bash)
```bash
az containerapp up \ 
  --name loe-power-schedule-app \ 
  --resource-group Loe-Sandbox \ 
  --location polandcentral \ 
  --environment 'loe-power-schedule-env' \ 
  --image loepowerschedule.azurecr.io/loepowerschedule:latest \ 
  --target-port 80 \ 
  --ingress external \ 
  --query properties.configuration.ingress.fqdn 
```