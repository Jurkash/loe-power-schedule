FROM mcr.microsoft.com/dotnet/aspnet:8.0-bookworm-slim-amd64 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["LoePowerSchedule/LoePowerSchedule.csproj", "LoePowerSchedule/"]
RUN dotnet restore "LoePowerSchedule/LoePowerSchedule.csproj"
COPY . .
WORKDIR "/src/LoePowerSchedule"
RUN dotnet build "LoePowerSchedule.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "LoePowerSchedule.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "LoePowerSchedule.dll"]
