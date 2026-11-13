# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY VitalReach.Web.csproj NuGet.Config ./
RUN dotnet restore VitalReach.Web.csproj --configfile NuGet.Config
COPY . .
RUN dotnet publish VitalReach.Web.csproj -c Release --no-restore -o /out /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
USER root
RUN apt-get update && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /out ./
# Startup ensures this directory exists, even with an external database path.
RUN mkdir -p /app/App_Data /data/keys /data/uploads/products \
    && chown -R app:app /app/App_Data /data
ENV ASPNETCORE_HTTP_PORTS=8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    ConnectionStrings__Catalog="Data Source=/data/vitalreach.db" \
    DataProtection__Path=/data/keys \
    DataProtection__ApplicationName=VitalReach.Production \
    ProductImages__Path=/data/uploads/products
USER app
EXPOSE 8080
HEALTHCHECK --interval=15s --timeout=5s --start-period=60s --retries=5 \
    CMD curl --fail --silent --show-error -H 'Host: vitalreachhub.com' http://127.0.0.1:8080/health || exit 1
ENTRYPOINT ["dotnet", "VitalReach.Web.dll"]
