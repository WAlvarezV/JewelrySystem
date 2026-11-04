<#
.SYNOPSIS
    Restaura una copia de seguridad (.dump) de PostgreSQL generada por el sistema o por GitHub Actions.
.DESCRIPTION
    Utiliza pg_restore para importar esquemas y datos limpios en la base de datos de Railway o PostgreSQL local.
.PARAMETER BackupFile
    Ruta local al archivo .dump a restaurar.
.PARAMETER DatabaseUrl
    URL de conexión a PostgreSQL (ej: postgresql://postgres:pass@proxy.rlwy.net:12345/railway).
    Si no se especifica, intentará leer la variable de entorno RAILWAY_DATABASE_URL o solicitarla interactivamente.
.PARAMETER Clean
    Si se especifica ($true por defecto), elimina y recrea los objetos existentes antes de restaurar (--clean --if-exists).
.EXAMPLE
    .\Restore-Database-Backup.ps1 -BackupFile "IntZaioNJewerly_20261104_050000.dump" -DatabaseUrl "postgresql://postgres:pass@...railway.app:5432/railway"
#>
param(
    [Parameter(Mandatory = $true, Position = 0)]
    [string]$BackupFile,

    [Parameter(Mandatory = $false, Position = 1)]
    [string]$DatabaseUrl = $env:RAILWAY_DATABASE_URL,

    [switch]$NoClean
)

$ErrorActionPreference = "Stop"

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "  Restaurador de Copias de Seguridad PostgreSQL           " -ForegroundColor Cyan
Write-Host "  Sistema JewerlySystem                                  " -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

# 1. Validar archivo de respaldo
if (-not (Test-Path $BackupFile)) {
    Write-Host "[ERROR] El archivo de respaldo no existe: $BackupFile" -ForegroundColor Red
    exit 1
}

$resolvedBackup = Resolve-Path $BackupFile
Write-Host "[OK] Archivo de respaldo: $resolvedBackup" -ForegroundColor Green

# 2. Validar URL de Base de Datos
if ([string]::IsNullOrWhiteSpace($DatabaseUrl)) {
    Write-Host ""
    $DatabaseUrl = Read-Host "Ingresa la URL de conexión PostgreSQL (ej: postgresql://postgres:password@host:port/database)"
}

if ([string]::IsNullOrWhiteSpace($DatabaseUrl)) {
    Write-Host "[ERROR] La URL de conexión a la base de datos es requerida." -ForegroundColor Red
    exit 1
}

# 3. Localizar pg_restore en el sistema
$pgRestore = $null
$possiblePaths = @(
    "pg_restore",
    "C:\Program Files\PostgreSQL\17\bin\pg_restore.exe",
    "C:\Program Files\PostgreSQL\16\bin\pg_restore.exe",
    "C:\Program Files\PostgreSQL\15\bin\pg_restore.exe",
    "C:\Program Files\PostgreSQL\14\bin\pg_restore.exe"
)

foreach ($p in $possiblePaths) {
    try {
        $cmd = Get-Command $p -ErrorAction SilentlyContinue
        if ($cmd) {
            $pgRestore = $cmd.Source
            break
        }
        if (Test-Path $p) {
            $pgRestore = $p
            break
        }
    } catch { }
}

if (-not $pgRestore) {
    Write-Host "[ERROR] No se encontró el binario 'pg_restore' en el sistema." -ForegroundColor Red
    Write-Host "Por favor instala PostgreSQL o añade la carpeta bin al PATH." -ForegroundColor Yellow
    exit 1
}

Write-Host "[OK] Utilizando pg_restore en: $pgRestore" -ForegroundColor Green

# 4. Confirmación de seguridad
Write-Host ""
Write-Host "[PRECAUCIÓN] Se restaurará la base de datos con los contenidos de '$BackupFile'." -ForegroundColor Yellow
$confirm = Read-Host "¿Estás seguro de continuar? (Escribe 'S' para proceder)"
if ($confirm -ne 'S' -and $confirm -ne 's') {
    Write-Host "Operación cancelada por el usuario." -ForegroundColor Gray
    exit 0
}

# 5. Ejecutar pg_restore
$restoreArgs = @(
    "--dbname=$DatabaseUrl",
    "--verbose",
    "--no-owner",
    "--no-privileges"
)

if (-not $NoClean) {
    $restoreArgs += "--clean"
    $restoreArgs += "--if-exists"
}

$restoreArgs += $resolvedBackup

Write-Host "[PROCESANDO] Ejecutando pg_restore..." -ForegroundColor Cyan
& $pgRestore $restoreArgs

if ($LASTEXITCODE -eq 0 -or $LASTEXITCODE -eq 1) {
    # pg_restore retorna 1 si hubo warnings no fatales (ej: objetos no existentes durante clean)
    Write-Host ""
    Write-Host "==========================================================" -ForegroundColor Green
    Write-Host "  ¡Restauración completada satisfactoriamente!            " -ForegroundColor Green
    Write-Host "==========================================================" -ForegroundColor Green
} else {
    Write-Host "[ERROR] pg_restore finalizó con código de error $LASTEXITCODE." -ForegroundColor Red
    exit $LASTEXITCODE
}
