# Manual de Importación: Base de Datos PostgreSQL (`IntZaioNJewerly`)

**Fecha del volcado:** 2026-11-02  
**Origen:** SQL Server BACPAC de Producción (`PomonaDb-20261102.bacpac`)  
**Destino:** PostgreSQL (Estructura Modular Monolith .NET 10)  

---

## 1. Contenido del Paquete de Exportación

| Archivo | Descripción | Uso Recomendado |
| :--- | :--- | :--- |
| `IntZaioNJewerly_20261102.sql` | Volcado SQL completo (esquema, 24 tablas, secuencias y datos). | Importación estándar mediante script o pgAdmin. |
| `IntZaioNJewerly_20261102_with_create_db.sql` | Volcado SQL que incluye `CREATE DATABASE`. | Importación directa conectándose a la base `postgres`. |
| `IntZaioNJewerly_20261102.dump` | Volcado binario comprimido de PostgreSQL (`pg_dump -F c`). | Restauración rápida mediante `pg_restore`. |
| `Import-Database.ps1` | Script automatizado para PowerShell (Windows/Linux/Mac). | **Método recomendado.** Ejecuta todo en un paso. |
| `import-database.bat` | Script batch para Windows (doble clic o CMD). | Alternativa para entornos Windows sin PowerShell moderno. |

---

## 2. Parámetros de Conexión por Defecto

* **Host:** `localhost`
* **Puerto:** `5432`
* **Base de datos:** `IntZaioNJewerly`
* **Usuario:** `postgres`
* **Contraseña:** `IntZaioN` (o la contraseña configurada en el cliente)

Cadena de conexión estándar para el archivo `appsettings.json` de la aplicación:
```text
Host=localhost;Port=5432;Database=IntZaioNJewerly;Username=postgres;Password=IntZaioN;
```

---

## 3. Instrucciones de Importación (3 Opciones)

### Opción A (Recomendada): Usando PowerShell
1. Abre una terminal de **PowerShell** en esta carpeta.
2. Si la contraseña de PostgreSQL del cliente es diferente a `IntZaioN`, especifícala con el parámetro `-Password`:
   ```powershell
   pwsh .\Import-Database.ps1 -Password "TuContraseña"
   ```
   *(Si la contraseña es la estándar `IntZaioN`, basta con ejecutar `.\Import-Database.ps1`)*.

---

### Opción B: Usando el archivo por lotes (.BAT)
1. Haz doble clic sobre `import-database.bat` o ejecútalo desde CMD:
   ```cmd
   import-database.bat
   ```
2. El script detectará la instalación de PostgreSQL, creará la base `IntZaioNJewerly` si no existe, importará los datos y mostrará el conteo final.

---

### Opción C: Mediante línea de comandos (`psql`)
Si prefieres ejecutar los comandos manualmente:

1. Crear la base de datos (si aún no existe):
   ```cmd
   set PGPASSWORD=IntZaioN
   psql -U postgres -h localhost -d postgres -c "CREATE DATABASE \"IntZaioNJewerly\" WITH ENCODING 'UTF8';"
   ```

2. Cargar el archivo SQL:
   ```cmd
   psql -U postgres -h localhost -d IntZaioNJewerly -f IntZaioNJewerly_20261102.sql
   ```

---

### Opción D: Usando pgAdmin 4 (GUI)
1. Abre **pgAdmin 4** y conéctate al servidor PostgreSQL.
2. Si la base de datos `IntZaioNJewerly` no existe:
   - Clic derecho en **Databases** -> **Create** -> **Database...**
   - Nombre: `IntZaioNJewerly`
   - Encoding: `UTF8`
   - Clic en **Save**.
3. Selecciona la base de datos `IntZaioNJewerly` y abre la herramienta de consultas (**Query Tool**).
4. Abre el archivo `IntZaioNJewerly_20261102.sql` (ícono de carpeta o `Ctrl + O`).
5. Ejecuta la consulta (`F5` o botón de Play).

---

## 4. Matriz de Validación de Registros Esperados

Al finalizar la importación, el conteo en PostgreSQL debe reflejar exactamente los datos de producción:

| Tabla | Registros Migrados | Observaciones |
| :--- | :---: | :--- |
| `IdentificationTypes` | **3** | Cédula, Extranjería, NIT |
| `Brands` | **13** | Marcas de relojería |
| `ItemTypes` | **7** | Tipos de artículos (Joya, Reloj, etc.) |
| `Persons` | **92** | Clientes y proveedores |
| `Items` | **1,125** | Catálogo general de artículos |
| `Jewelry` | **801** | Joyas en stock |
| `Watches` | **324** | Relojes en stock |
| `DailyRecords` | **538** | Arqueo histórico de movimientos de caja |
| `Contracts` | **0** | Listo para nuevos contratos |
| `Payments` | **0** | Listo para nuevos pagos |
| `Invoices` | **0** | Listo para nuevas facturas |
| `AspNetUsers` | **2** | `admin@joyeriapomona.com` (producción) y `walvarez@ingnova.team` (soporte) |
| `AspNetRoles` | **6** | Roles del sistema (`Admin`, `Administrador`, `Supervisor`, etc.) |
| `AspNetUserRoles` | **4** | Roles administrativos asignados a ambos usuarios |
| `AppModules` | **6** | Módulos del Monolito Modular |
| `MenuItems` | **20** | Navegación SI.SIOT |
| `RolePermissions` | **40** | Matriz de permisos |

---

## 5. Verificación de Secuencias de Autoincremento
Todas las secuencias de autoincremento (`SERIAL / IDENTITY`) han sido sincronizadas al último ID generado, garantizando que los nuevos registros creados desde la aplicación (`Items`, `Jewelry`, `Watches`, etc.) no colisionen con IDs existentes.
