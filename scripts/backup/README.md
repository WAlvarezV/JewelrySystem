# 📦 Copia de Seguridad Automatizada de PostgreSQL a Google Drive

Este módulo implementa la **Opción 1**: Respaldos automáticos y programados de la base de datos PostgreSQL alojada en **Railway** hacia una carpeta designada de **Google Drive**, utilizando **GitHub Actions** con una política de retención automática que elimina las copias más antiguas (por defecto > 15 días).

---

## 🏛️ Arquitectura del Flujo

```
┌─────────────────────────────────┐
│     Railway PostgreSQL          │
│ (Base de datos en producción)   │
└────────────────┬────────────────┘
                 │ Conexión segura pública
                 ▼
┌─────────────────────────────────┐
│      GitHub Actions Runner      │
│  - pg_dump formato custom (-Fc) │
│  - Compresión de datos (nivel 9)│
│  - Verificación SHA-256         │
│  - Guardado temporal en Artifact│
└────────────────┬────────────────┘
                 │ Rclone + Google Cloud Service Account
                 ▼
┌─────────────────────────────────┐
│     Google Drive (Carpeta)      │
│  - Subida de copia con timestamp│
│  - Depuración automática de     │
│    respaldos > 15 días          │
└─────────────────────────────────┘
```

---

## ⏱️ Programación

- **Automático (Cron)**: Se ejecuta todos los días a las `05:00 UTC` (**00:00 hora Colombia COT**).
- **Manual (Bajo demanda)**: Puedes disparar un respaldo en cualquier momento desde la pestaña **Actions** de GitHub pulsando **Run workflow**, con la opción de cambiar los días de retención (ej. 7, 15, 30 días).

---

## 🔑 Secretos Requeridos en GitHub

Para que el flujo funcione, debes registrar los siguientes 3 secretos en tu repositorio de GitHub:  
👉 **Repositorio en GitHub** > **Settings** > **Secrets and variables** > **Actions** > **New repository secret**:

| Nombre del Secreto | Descripción | Ejemplo / Formato |
|---|---|---|
| `RAILWAY_DATABASE_URL` | URL de conexión pública a PostgreSQL provista por Railway. | `postgresql://postgres:pass@proxy.rlwy.net:12345/railway` |
| `GDRIVE_SERVICE_ACCOUNT_KEY` | Contenido completo del archivo JSON de la Service Account de Google Cloud. | `{"type": "service_account", "project_id": "...", ...}` |
| `GDRIVE_FOLDER_ID` | Identificador único de la carpeta de Google Drive donde se guardarán los volcados. | `1a2B3c4D5e6F7g8H9i0J...` |

---

## 🚀 Guía de Configuración Paso a Paso (5 Minutos)

### Paso 1: Obtener la URL Pública de PostgreSQL en Railway
1. Ingresa a tu panel de [Railway](https://railway.app/).
2. Haz clic en el servicio de tu base de datos **PostgreSQL**.
3. Ve a la pestaña **Connect** (o **Variables**).
4. En la sección **Public Networking** (Red Pública), copia la variable `DATABASE_PUBLIC_URL` o `DATABASE_URL`.
   > [!IMPORTANT]
   > Asegúrate de usar la URL que incluye el host público (ej: `...proxy.rlwy.net` o `...railway.app`) y no la URL interna (`*.railway.internal`), ya que GitHub Actions se ejecuta fuera de la red privada de Railway.

---

### Paso 2: Crear la Service Account en Google Cloud (100% Gratuito)
Una *Service Account* permite a GitHub Actions conectarse a Google Drive sin necesidad de autenticarse en el navegador ni lidiar con tokens que expiran cada 7 días.

1. Ve a [Google Cloud Console](https://console.cloud.google.com/).
2. Crea un proyecto nuevo (ejemplo: `JewerlySystem-Backups`) o selecciona uno existente.
3. En el menú lateral, ve a **APIs y servicios** > **Biblioteca**.
4. Busca **Google Drive API**, selecciónala y haz clic en **Habilitar** (Enable).
5. Ve a **IAM y administración** > **Cuentas de servicio** (Service Accounts).
6. Haz clic en **Crear cuenta de servicio**:
   - Nombre: `gdrive-backup-agent`
   - ID: `gdrive-backup-agent`
   - Haz clic en **Crear y continuar** y luego en **Listo**.
7. En el listado de cuentas de servicio, haz clic sobre el correo de la cuenta que acabas de crear (ej: `gdrive-backup-agent@jewerlysystem-backups.iam.gserviceaccount.com`).
   - Copia ese correo electrónico en tu portapapeles.
8. Ve a la pestaña **Claves** (Keys) > **Agregar clave** > **Crear clave nueva**.
9. Selecciona el formato **JSON** y haz clic en **Crear**.
   - Se descargará automáticamente un archivo `.json` a tu computadora.
   - Abre ese archivo con el Bloc de notas o VS Code. Copiarás todo su contenido más adelante.

---

### Paso 3: Crear la Carpeta en Google Drive y Compartirla
1. Abre tu [Google Drive](https://drive.google.com/) personal o de la empresa.
2. Crea una carpeta nueva donde quieras almacenar los respaldos (ejemplo: `Respaldos_JewerlySystem`).
3. Haz clic derecho sobre la carpeta > **Compartir** > **Compartir**.
4. En el campo de invitados, **pega el correo de la Service Account** copiado en el Paso 2 (`gdrive-backup-agent@...iam.gserviceaccount.com`).
5. Asigna el rol de **Editor** (para que pueda subir archivos y eliminar las copias viejas).
6. Desmarca la casilla *"Notificar a las personas"* y haz clic en **Compartir**.
7. Abre la carpeta en Google Drive y observa la URL en la barra de direcciones de tu navegador:
   ```
   https://drive.google.com/drive/folders/1ABC_XYZ1234567890abcdef
   ```
   El **Folder ID** es el texto al final después de `/folders/` (en este ejemplo: `1ABC_XYZ1234567890abcdef`).

---

### Paso 4: Cargar los Secretos en GitHub
1. Abre el repositorio del proyecto en GitHub.
2. Ve a **Settings** > **Secrets and variables** > **Actions**.
3. Haz clic en **New repository secret** y crea los 3 secretos:
   - `RAILWAY_DATABASE_URL`: Pega la URL pública obtenida en el Paso 1.
   - `GDRIVE_SERVICE_ACCOUNT_KEY`: Pega todo el contenido del archivo JSON del Paso 2.
   - `GDRIVE_FOLDER_ID`: Pega el ID de la carpeta obtenido en el Paso 3.

---

### Paso 5: Ejecutar la Primera Prueba Manual
1. En GitHub, ve a la pestaña **Actions**.
2. En la lista izquierda, selecciona **PostgreSQL Database Backup to Google Drive**.
3. Haz clic en el botón desplegable **Run workflow**.
4. Deja la retención por defecto (`15`) y haz clic en el botón verde **Run workflow**.
5. Abre la ejecución para ver el proceso en tiempo real:
   - Verificará la conexión con Railway.
   - Realizará el volcado binario comprimido con `pg_dump`.
   - Subirá el archivo a tu carpeta de Google Drive.
   - Evaluará la política de retención.
   - Mostrará un reporte en pantalla con el tamaño y hash de integridad.
6. Entra a tu carpeta de Google Drive: ¡allí verás tu archivo `IntZaioNJewerly_YYYYMMDD_HHMMSS.dump`!

---

## 🧹 Política de Retención Automática

Para evitar que tu Google Drive se llene con el paso de los meses:
- El workflow ejecuta el comando `rclone delete --min-age 15d gdrive:`.
- Cualquier copia de seguridad que tenga **más de 15 días de antigüedad se elimina permanentemente** de forma automática.
- Siempre mantendrás los últimos 15 días continuos de copias de seguridad disponibles para recuperación.
- Si deseas cambiar este periodo (por ejemplo a 30 días), puedes ajustar el valor por defecto en `.github/workflows/database-backup.yml` o ingresarlo manualmente al disparar el workflow.

---

## 🔄 Cómo Restaurar una Copia de Seguridad

Si alguna vez necesitas restaurar una base de datos a partir de un archivo `.dump`:

### Opción A: Usando el script incluido en PowerShell
```powershell
cd scripts/backup
.\Restore-Database-Backup.ps1 -BackupFile "IntZaioNJewerly_20261104_050000.dump" -DatabaseUrl "postgresql://postgres:pass@...railway.app:5432/railway"
```

### Opción B: Usando el comando nativo `pg_restore`
```bash
pg_restore -d "postgresql://postgres:pass@host:port/database" \
  --clean \
  --if-exists \
  --no-owner \
  --no-privileges \
  --verbose \
  "IntZaioNJewerly_20261104_050000.dump"
```

---

## 💻 Respaldo Manual Local (Opcional)

Si un desarrollador desea generar un volcado localmente a su máquina sin esperar a GitHub Actions:
```powershell
cd scripts/backup
.\Manual-Backup.ps1 -DatabaseUrl "postgresql://postgres:pass@roundhouse.proxy.rlwy.net:12345/railway"
```
El archivo se guardará automáticamente en `scripts/backup/dumps/`.
