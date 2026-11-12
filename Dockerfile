# Stage 1: Build & Publish
FROM mcr.microsoft.com/dotnet/sdk:11.0 AS build
WORKDIR /src

# Copy CPM and build properties
COPY Directory.Build.props ./
COPY Directory.Packages.props ./

# Copy solution source code
COPY src/ ./src/

# Restore and publish JewerlySystem.Web
WORKDIR /src/src/Host/JewerlySystem.Web
RUN dotnet publish "JewerlySystem.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Runtime (Hardened Non-Root Container - ISO 27001 A.8.9 / OWASP A02)
FROM mcr.microsoft.com/dotnet/aspnet:11.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080 \
    PORT=8080 \
    DOTNET_EnableDiagnostics=0 \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false \
    LC_ALL=es_CO.UTF-8 \
    LANG=es_CO.UTF-8

EXPOSE 8080

COPY --from=build --chown=$APP_UID:$APP_UID /app/publish .

# Run as non-privileged app user (UID 1654)
USER $APP_UID

ENTRYPOINT ["dotnet", "JewerlySystem.Web.dll"]
