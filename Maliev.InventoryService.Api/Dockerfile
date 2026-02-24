# syntax=docker/dockerfile:1.4
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["nuget.config", "."]
COPY ["Maliev.InventoryService.Api/Maliev.InventoryService.Api.csproj", "Maliev.InventoryService.Api/"]
COPY ["Maliev.InventoryService.Data/Maliev.InventoryService.Data.csproj", "Maliev.InventoryService.Data/"]

RUN --mount=type=secret,id=nuget_username \
    --mount=type=secret,id=nuget_password \
    NUGET_USERNAME=$(cat /run/secrets/nuget_username) \
    NUGET_PASSWORD=$(cat /run/secrets/nuget_password) \
    dotnet restore "./Maliev.InventoryService.Api/Maliev.InventoryService.Api.csproj"

COPY . .
WORKDIR "/src/Maliev.InventoryService.Api"
RUN dotnet build "./Maliev.InventoryService.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Maliev.InventoryService.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
RUN chown -R app:app /app
USER app
COPY --chown=app:app --from=publish /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD curl -f http://localhost:8080/inventory/liveness || exit 1

ENTRYPOINT ["dotnet", "Maliev.InventoryService.Api.dll"]
