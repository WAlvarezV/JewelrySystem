<#
.SYNOPSIS
    Genera un respaldo local (.dump) de la base de datos PostgreSQL de Railway usando pg_dump.
.DESCRIPTION
    Conecta a la base de datos remota usando la URL pública y descarga un volcado comprimido (-Fc).
.PARAMETER DatabaseUrl
    URL pública de PostgreSQL en Railway. Si no se indica, utiliza $env:RAILWAY_DATABASE_URL o la solicita.
.PARAMETER OutputFolder
    Directorio donde se almacenará el archivo .dump (por defecto: scripts/backup/dumps).
.PARAMETER Prefix
    Prefijo del archivo de respaldo (por defecto: IntZaioNJewerly).
.EXAMPLE
    .\Manual-Backup.ps1 -DatabaseUrl "postgresql://postgres:password@roundhouse.proxy.rlwy.net:12345/railway"
#>
param(
    [Parameter(Mandatory = $false, Position = 0)]
    [string]$DatabaseUrl = $env:RAILWAY_DATABASE_URL,

    [Parameter(Mandatory = $false)]
    [string]$OutputFolder = "",

    [Parameter(Mandatory = $false)]
    [string]$Prefix = "IntZaioNJewerly"
)

$ErrorActionPreference = "Stop"

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "  Generador Manual de Respaldo PostgreSQL (pg_dump)       " -ForegroundColor Cyan
Write-Host "  Sistema JewerlySystem                                  " -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

# 1. Validar URL de base de datos
if ([string]::IsNullOrWhiteSpace($DatabaseUrl)) {
    Write-Host ""
    $DatabaseUrl = Read-Host "Ingresa la URL pública de PostgreSQL de Railway (ej: postgresql://postgres:...@proxy.rlwy.net:.../railway)"
}

if ([string]::IsNullOrWhiteSpace($DatabaseUrl)) {
    Write-Host "[ERROR] La URL de la base de datos es requerida." -ForegroundColor Red
    exit 1
}

# 2. Localizar pg_dump
$pgDump = $null
$possiblePaths = @(
    "pg_dump",
    "C:\Program Files\PostgreSQL\17\bin\pg_dump.exe",
    "C:\Program Files\PostgreSQL\16\bin\pg_dump.exe",
    "C:\Program Files\PostgreSQL\15\bin\pg_dump.exe",
    "C:\Program Files\PostgreSQL\14\bin\pg_dump.exe"
)

foreach ($p in $possiblePaths) {
    try {
        $cmd = Get-Command $p -ErrorAction SilentlyContinue
        if ($cmd) {
            $pgDump = $cmd.Source
            break
        }
        if (Test-Path $p) {
            $pgDump = $p
            break
        }
    } catch { }
}

if (-not $pgDump) {
    Write-Host "[ERROR] No se encontró 'pg_dump' en el sistema." -ForegroundColor Red
    Write-Host "Instala PostgreSQL para Windows o añade la carpeta bin al PATH del sistema." -ForegroundColor Yellow
    exit 1
}

# 3. Preparar directorio de salida
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
if ([string]::IsNullOrWhiteSpace($OutputFolder)) {
    $OutputFolder = Join-Path $scriptDir "dumps"
}

if (-not (Test-Path $OutputFolder)) {
    New-Item -ItemType Directory -Path $OutputFolder -Force | Out-Null
}

$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$filename = "${Prefix}_${timestamp}.dump"
$destinationPath = Join-Path $OutputFolder $filename

Write-Host "[PROCESANDO] Generando respaldo en formato Custom comprimido (-Fc)..." -ForegroundColor Cyan
Write-Host "Destino: $destinationPath" -ForegroundColor Gray

& $pgDump $DatabaseUrl --format=custom --compress=9 --verbose --file="$destinationPath"

if ($LASTEXITCODE -eq 0 -and (Test-Path $destinationPath)) {
    $fileItem = Get-Item $destinationPath
    $sizeKb = [Math]::Round($fileItem.Length / 1KB, 2)
    Write-Host ""
    Write-Host "==========================================================" -ForegroundColor Green
    Write-Host "  ¡Respaldo generado exitosamente!                       " -ForegroundColor Green
    Write-Host "  Archivo : $filename" -ForegroundColor Green
    Write-Host "  Tamaño  : $sizeKb KB ($($fileItem.Length) bytes)" -ForegroundColor Green
    Write-Host "  Ruta    : $destinationPath" -ForegroundColor Green
    Write-Host "==========================================================" -ForegroundColor Green
} else {
    Write-Host "[ERROR] Falló la ejecución de pg_dump." -ForegroundColor Red
    exit 1
}
