# Stage 1: Build & Publish
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy CPM and build properties
COPY Directory.Build.props ./
COPY Directory.Packages.props ./

# Copy solution source code
COPY src/ ./src/

# Restore and publish JewerlySystem.Web
WORKDIR /src/src/Host/JewerlySystem.Web
RUN dotnet publish "JewerlySystem.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
ENV PORT=8080
EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "JewerlySystem.Web.dll"]
