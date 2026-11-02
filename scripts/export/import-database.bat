@echo off
chcp 65001 >nul
echo ==========================================================
echo   Importador Automatico de Base de Datos PostgreSQL
echo   Sistema JewerlySystem - Base de Datos: IntZaioNJewerly
echo ==========================================================
echo.

set PGHOST=localhost
set PGPORT=5432
set PGDATABASE=IntZaioNJewerly
set PGUSER=postgres
set PGPASSWORD=IntZaioN

REM Buscar psql.exe en versiones conocidas
set PSQL_EXE=
if exist "C:\Program Files\PostgreSQL\18\bin\psql.exe" set PSQL_EXE=C:\Program Files\PostgreSQL\18\bin\psql.exe
if not defined PSQL_EXE if exist "C:\Program Files\PostgreSQL\17\bin\psql.exe" set PSQL_EXE=C:\Program Files\PostgreSQL\17\bin\psql.exe
if not defined PSQL_EXE if exist "C:\Program Files\PostgreSQL\16\bin\psql.exe" set PSQL_EXE=C:\Program Files\PostgreSQL\16\bin\psql.exe
if not defined PSQL_EXE if exist "C:\Program Files\PostgreSQL\15\bin\psql.exe" set PSQL_EXE=C:\Program Files\PostgreSQL\15\bin\psql.exe
if not defined PSQL_EXE if exist "C:\Program Files\PostgreSQL\14\bin\psql.exe" set PSQL_EXE=C:\Program Files\PostgreSQL\14\bin\psql.exe
if not defined PSQL_EXE (
    where psql >nul 2>nul
    if %ERRORLEVEL% equ 0 set PSQL_EXE=psql
)

if not defined PSQL_EXE (
    echo [ERROR] No se encontro psql.exe en las rutas estandar.
    echo Asegurese de tener PostgreSQL instalado o anada la carpeta bin al PATH.
    pause
    exit /b 1
)

echo [OK] Motor PostgreSQL encontrado: %PSQL_EXE%
echo [INFO] Servidor: %PGHOST%:%PGPORT% ^| Usuario: %PGUSER% ^| Base de Datos: %PGDATABASE%
echo.

echo [1/3] Creando base de datos si no existe...
"%PSQL_EXE%" -h %PGHOST% -p %PGPORT% -U %PGUSER% -d postgres -c "SELECT 1 FROM pg_database WHERE datname = '%PGDATABASE%';" | findstr "1" >nul
if %ERRORLEVEL% neq 0 (
    "%PSQL_EXE%" -h %PGHOST% -p %PGPORT% -U %PGUSER% -d postgres -c "CREATE DATABASE \"%PGDATABASE%\" WITH ENCODING 'UTF8';"
    echo Base de datos creada exitosamente.
) else (
    echo La base de datos ya existe.
)

echo.
echo [2/3] Importando volcado SQL IntZaioNJewerly_20261102.sql...
"%PSQL_EXE%" -h %PGHOST% -p %PGPORT% -U %PGUSER% -d %PGDATABASE% -q -f "%~dp0IntZaioNJewerly_20261102.sql"

echo.
echo [3/3] Validando registros cargados...
"%PSQL_EXE%" -h %PGHOST% -p %PGPORT% -U %PGUSER% -d %PGDATABASE% -c "SELECT 'Items' AS Tabla, COUNT(*) AS Total FROM \"Items\" UNION ALL SELECT 'Jewelry', COUNT(*) FROM \"Jewelry\" UNION ALL SELECT 'Watches', COUNT(*) FROM \"Watches\" UNION ALL SELECT 'Persons', COUNT(*) FROM \"Persons\" UNION ALL SELECT 'DailyRecords', COUNT(*) FROM \"DailyRecords\" UNION ALL SELECT 'AspNetUsers', COUNT(*) FROM \"AspNetUsers\";"

echo.
echo ==========================================================
echo   PROCESO FINALIZADO CON EXITO
echo ==========================================================
pause
