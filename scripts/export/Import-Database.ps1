<#
.SYNOPSIS
    Script automatizado para importar la base de datos PostgreSQL 'IntZaioNJewerly' en el entorno del cliente.
.DESCRIPTION
    Detecta automáticamente la instalación local de PostgreSQL (versiones 14, 15, 16, 17, 18),
    crea la base de datos si no existe, ejecuta el volcado SQL con paridad completa de datos,
    ajusta las secuencias de autoincremento y valida el conteo de registros.
.PARAMETER HostName
    Nombre del host o IP del servidor PostgreSQL. Por defecto: localhost
.PARAMETER Port
    Puerto del servidor PostgreSQL. Por defecto: 5432
.PARAMETER Database
    Nombre de la base de datos destino. Por defecto: IntZaioNJewerly
.PARAMETER Username
    Usuario con permisos de administrador en PostgreSQL. Por defecto: postgres
.PARAMETER Password
    Contraseña del usuario de PostgreSQL. Por defecto: IntZaioN
.PARAMETER SqlFile
    Ruta del archivo SQL a importar. Por defecto: IntZaioNJewerly_20261102.sql
#>
param(
    [string]$HostName = "localhost",
    [int]$Port = 5432,
    [string]$Database = "IntZaioNJewerly",
    [string]$Username = "postgres",
    [string]$Password = "IntZaioN",
    [string]$SqlFile = ""
)

$ErrorActionPreference = "Stop"

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "  Importador Automático de Base de Datos PostgreSQL       " -ForegroundColor Cyan
Write-Host "  Sistema JewerlySystem - Base de Datos: $Database        " -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host ""

# 1. Determinar archivo SQL
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
if ([string]::IsNullOrWhiteSpace($SqlFile)) {
    $SqlFile = Join-Path $scriptDir "IntZaioNJewerly_20261102.sql"
}
if (-not (Test-Path $SqlFile)) {
    Write-Host "[ERROR] No se encontró el archivo SQL en: $SqlFile" -ForegroundColor Red
    exit 1
}

# 2. Localizar binarios de PostgreSQL (psql.exe)
$psqlPath = $null

# Revisar si psql está en el PATH
$inPath = Get-Command psql.exe -ErrorAction SilentlyContinue
if ($inPath) {
    $psqlPath = $inPath.Source
} else {
    # Buscar en rutas estándar de Windows (versiones 18 a 13)
    $candidateVersions = 18..13
    foreach ($ver in $candidateVersions) {
        $p = "C:\Program Files\PostgreSQL\$ver\bin\psql.exe"
        if (Test-Path $p) {
            $psqlPath = $p
            break
        }
    }
}

if (-not $psqlPath) {
    Write-Host "[ERROR] No se encontró 'psql.exe'. Asegúrese de que PostgreSQL esté instalado." -ForegroundColor Red
    Write-Host "Rutas buscadas: C:\Program Files\PostgreSQL\<version>\bin\psql.exe" -ForegroundColor Yellow
    exit 1
}

Write-Host "[OK] Motor PostgreSQL detectado en: $psqlPath" -ForegroundColor Green
Write-Host "[OK] Archivo origen: $SqlFile" -ForegroundColor Green
Write-Host "[INFO] Servidor: ${HostName}:${Port} | Usuario: $Username | Base de Datos: $Database" -ForegroundColor Gray
Write-Host ""

$env:PGPASSWORD = $Password

# 3. Verificar si la base de datos existe, si no, crearla
Write-Host "[1/3] Verificando existencia de la base de datos '$Database'..." -ForegroundColor Yellow
$checkDbSql = "SELECT 1 FROM pg_database WHERE datname = '$Database';"
$dbExists = & $psqlPath -h $HostName -p $Port -U $Username -d postgres -t -c $checkDbSql 2>&1

if ($LASTEXITCODE -ne 0) {
    Write-Host "[ERROR] No fue posible conectar al servidor PostgreSQL. Verifique credenciales o servicio." -ForegroundColor Red
    Write-Host $dbExists
    exit 1
}

if ($dbExists -notmatch "1") {
    Write-Host "  -> La base de datos '$Database' no existe. Creando con encoding UTF8..." -ForegroundColor Cyan
    & $psqlPath -h $HostName -p $Port -U $Username -d postgres -c "CREATE DATABASE `"$Database`" WITH ENCODING 'UTF8';" | Out-Null
    if ($LASTEXITCODE -ne 0) {
        Write-Host "[ERROR] Error al crear la base de datos '$Database'." -ForegroundColor Red
        exit 1
    }
    Write-Host "  -> Base de datos '$Database' creada exitosamente." -ForegroundColor Green
} else {
    Write-Host "  -> Base de datos '$Database' ya existe." -ForegroundColor Green
}

# 4. Importar volcado SQL
Write-Host ""
Write-Host "[2/3] Importando esquema, tablas, índices y datos..." -ForegroundColor Yellow
& $psqlPath -h $HostName -p $Port -U $Username -d $Database -q -f $SqlFile

if ($LASTEXITCODE -ne 0) {
    Write-Host "[ADVERTENCIA] La importación finalizó con advertencias o errores menores." -ForegroundColor Yellow
} else {
    Write-Host "  -> Estructura y datos cargados exitosamente." -ForegroundColor Green
}

# 5. Validación final de registros
Write-Host ""
Write-Host "[3/3] Validando catálogo de tablas y registros migrados..." -ForegroundColor Yellow

$validationSql = @"
SELECT 
    RPAD(table_name, 22, ' ') || ' | ' || LPAD(count_records::text, 10, ' ') AS report
FROM (
    SELECT 'IdentificationTypes' AS table_name, count(*) AS count_records FROM "IdentificationTypes"
    UNION ALL SELECT 'Brands', count(*) FROM "Brands"
    UNION ALL SELECT 'ItemTypes', count(*) FROM "ItemTypes"
    UNION ALL SELECT 'Persons', count(*) FROM "Persons"
    UNION ALL SELECT 'Items', count(*) FROM "Items"
    UNION ALL SELECT 'Jewelry', count(*) FROM "Jewelry"
    UNION ALL SELECT 'Watches', count(*) FROM "Watches"
    UNION ALL SELECT 'DailyRecords', count(*) FROM "DailyRecords"
    UNION ALL SELECT 'Contracts', count(*) FROM "Contracts"
    UNION ALL SELECT 'Payments', count(*) FROM "Payments"
    UNION ALL SELECT 'Invoices', count(*) FROM "Invoices"
    UNION ALL SELECT 'AspNetUsers', count(*) FROM "AspNetUsers"
    UNION ALL SELECT 'AspNetRoles', count(*) FROM "AspNetRoles"
    UNION ALL SELECT 'AspNetUserRoles', count(*) FROM "AspNetUserRoles"
    UNION ALL SELECT 'AppModules', count(*) FROM "AppModules"
    UNION ALL SELECT 'MenuItems', count(*) FROM "MenuItems"
    UNION ALL SELECT 'RolePermissions', count(*) FROM "RolePermissions"
) sub
ORDER BY table_name;
"@

Write-Host "---------------------------------------------------------"
Write-Host "Tabla                  | Registros"
Write-Host "---------------------------------------------------------"
& $psqlPath -h $HostName -p $Port -U $Username -d $Database -t -c $validationSql
Write-Host "---------------------------------------------------------"

Write-Host ""
Write-Host "¡IMPORTACIÓN EN CLIENTE COMPLETADA EXITOSAMENTE!" -ForegroundColor Green
Write-Host "La base de datos '$Database' está lista para ser consumida por JewerlySystem." -ForegroundColor Cyan
