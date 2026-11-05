# 📖 Guía de Configuración e Integración de Manuales de Usuario — JewelrySystem

Esta guía documenta el estándar y procedimiento para documentar módulos, agregar nuevos manuales de usuario o registrar nuevas secciones de ayuda contextual en el sistema **JewelrySystem**.

---

## 🎯 Arquitectura de Documentación y Ayuda

El sistema implementa la misma arquitectura documental estandarizada de **SI.SIOT**:

1. **Archivos Markdown y Recursos (`wwwroot/docs/modules/<Modulo>/`)**: Archivos de manuales en formato Markdown con sus imágenes correspondientes.
2. **Registro Centralizado de Rutas (`help-routes.json`)**: Asocia URLs Blazor con manuales y secciones específicas.
3. **Herramienta de Capacitación Imprimible (`tools/manual-capacitacion/`)**: Generador automatizado de documentos Word (.docx) y PDF con índice y paginación real.

---

## 🚀 Pasos para Integrar un Nuevo Manual o Sección

### 📁 Paso 1: Ubicar el archivo `.md` y sus imágenes

Cree una subcarpeta para el módulo dentro de `wwwroot/docs/modules/` y coloque allí el archivo `ManualUsuario.md` junto con su carpeta `images`:

```text
src/Host/JewerlySystem.Web/wwwroot/docs/modules/
└── Inventory/                            ← Nombre del módulo
    ├── ManualUsuario.md                  ← Manual en formato Markdown
    └── images/                           ← Imágenes referenciadas en el manual
        ├── catalogo-joyeria.png
        └── historial-auditoria.png
```

> **Formato de referencia de imágenes en Markdown:**  
> Referencie las imágenes de forma relativa simple: `![Historial y auditoría](images/historial-auditoria.png)`.

---

### 📌 Paso 2: Identificar Secciones con `id` en el Markdown

Para permitir que el sistema navegue a la sección específica, asigne atributos `id="..."` a los bloques `<details>` o elementos `<div>`:

```markdown
<details id="proceso-historial">
<summary><h2 class="doc-process-title">🕒 Proceso: Consulta de Historial y Auditoría</h2></summary>
<!-- fuente-ui: src/Modules/Inventory/JewerlySystem.Modules.Inventory.Presentation/Pages/InventoryList.razor -->

<br/>

### 🎯 Objetivo del Proceso
Permitir a los administradores y auditores inspeccionar la trazabilidad completa...

<div id="paso-1-apertura"></div>

### 📌 Paso 1: Apertura del Historial
Haga clic en el botón con icono de reloj en la columna de acciones...

</details>
```

---

### ⚙️ Paso 3: Registrar la Ruta en `help-routes.json`

En `src/Host/JewerlySystem.Web/wwwroot/docs/help-routes.json`:

```json
{
  "/inventory": {
    "moduleName": "Inventory",
    "documentPath": "docs/modules/Inventory/ManualUsuario.md",
    "sectionAnchor": "proceso-catalogo",
    "customTitle": "Ayuda — Catálogo de Inventario"
  }
}
```

---

## ✍️ Estándares de Redacción

1. **Describa los controles por su nombre visible exacto** en negrita y comillas: **"Historial"**, **"Editar Joya"**, **"Descargar / Vender"**.
2. **Convención de pasos:** Utilice `### 📌 Paso k: Título` y `#### 🔹 N.k. Título` para que el analizador de Word los identifique y numere automáticamente.
3. **Cajas de advertencia:** Utilice recuadros con formato Blockquote:
   - `> 💡 **Nota:** Información relevante...`
   - `> ⚠️ **Importante:** Reglas de negocio obligatorias...`
   - `> 🔒 **Seguridad:** Control de permisos o bitácora de auditoría...`
4. **Problemas frecuentes:** Finalice cada proceso con la sección `### ❓ Problemas Frecuentes` en formato pregunta en negrita y respuesta.

---

## 🚫 Sin Dependencia de Microsoft Graph ni Servicios en la Nube

A diferencia de arquitecturas que delegan la conversión de documentos a Microsoft Graph API o servicios externos en la nube:
* **No se generan endpoints de exportación en línea dependientes de Microsoft Graph ni Azure.**
* La documentación se consulta directamente en la aplicación web en formato Markdown vivo e interactivo.
* La generación de manuales impresos para capacitación (.docx / .pdf) se efectúa de forma local y autónoma mediante la herramienta `tools/manual-capacitacion/`, sin depender de credenciales o costos de Microsoft Graph.
