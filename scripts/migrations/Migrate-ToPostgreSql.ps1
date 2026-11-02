<#
.SYNOPSIS
    Migra los datos desde Microsoft SQL Server (PomonaDb) hacia PostgreSQL (IntZaioNJewerly).
.DESCRIPTION
    Este script compila y ejecuta la utilidad DataMigrator.
    Es completamente re-ejecutable (idempotente) para soportar futuras cargas de nuevos archivos .bacpac.
.PARAMETER SqlServerConnectionString
    Cadena de conexión de origen. Por defecto: Server=(localdb)\MSSQLLocalDB;Database=PomonaDb_Source;Trusted_Connection=True;TrustServerCertificate=True;
.PARAMETER PostgreSqlConnectionString
    Cadena de conexión de destino. Por defecto: Host=localhost;Port=5432;Database=IntZaioNJewerly;Username=postgres;Password=IntZaioN;
#>
param(
    [string]$SqlServerConnectionString = "Server=(localdb)\MSSQLLocalDB;Database=PomonaDb_Source;Trusted_Connection=True;TrustServerCertificate=True;",
    [string]$PostgreSqlConnectionString = "Host=localhost;Port=5432;Database=IntZaioNJewerly;Username=postgres;Password=IntZaioN;"
)

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectPath = Join-Path $scriptDir "DataMigrator\DataMigrator.csproj"

Write-Host "Iniciando migrador de datos JewerlySystem..." -ForegroundColor Cyan
dotnet run --project $projectPath -- $SqlServerConnectionString $PostgreSqlConnectionString
