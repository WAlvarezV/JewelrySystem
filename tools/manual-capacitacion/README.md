# Manual de Capacitación y Documentación Imprimible (Word y PDF)

Genera la versión imprimible y formal para capacitación del manual de usuario a partir del mismo archivo `ManualUsuario.md` que utiliza el sistema en línea (`src/Host/JewerlySystem.Web/wwwroot/docs/modules/Inventory/`).

El archivo `.md` es la **única fuente de verdad**. El Word (.docx) y el PDF se **generan automáticamente**:
si se requiere corregir un paso o incorporar una nueva funcionalidad, se modifica el Markdown y se vuelve a ejecutar la generación.

---

## 📋 Requisitos del Entorno

| Paso | Requisito |
| :--- | :--- |
| Generar el `.docx` | Node.js 18 o superior |
| Actualizar índice, referencias de página y exportar a PDF | Windows con Microsoft Word instalado |

---

## 🚀 Uso Rápido

```powershell
cd tools/manual-capacitacion
npm install          # Solo la primera vez
npm run inventario   # Genera el .docx y el .pdf en out/
```

### Comandos Disponibles

| Comando | Acción |
| :--- | :--- |
| `npm run build` o `node build-manual.js inventario` | Genera únicamente el documento Word `.docx` en `out/`. |
| `npm run pdf` | Abre Word mediante automatización COM, actualiza el índice (TOC) y referencias "pág. X" en 2 pasadas, y exporta el archivo `.pdf`. |
| `npm run preview` | Igual que `pdf`, y además genera imágenes PNG de cada página en `out/pages/` para revisión rápida. |
| `npm run inventario` | Ejecuta el flujo completo para el módulo de inventario (compilación del `.docx` y conversión a `.pdf`). |

---

## 🏗️ Arquitectura de Generación

```text
ManualUsuario.md ──► build-manual.js ──► out/*.docx ──► finalize.ps1 (Word) ──► out/*.docx paginado + out/*.pdf
       ▲
       │
config/inventario.js  (portada, mapa de procesos, flujos, glosario, encabezado)
```

1. **`build-manual.js`**: Lee y procesa las convenciones de `ManualUsuario.md`:
   - Bloques `<details id="...">` y títulos `<summary><h2>...</h2></summary>` como capítulos numerados en hoja nueva.
   - Tablas de roles y prerrequisitos.
   - Diagrama "Resumen del proceso" y tabla de navegación rápida.
   - Secciones `### 📌 Paso k:` y subsecciones `#### 🔹 ...`.
   - Cajas de texto estructuradas para Notas, Alertas e Importante.
   - Figuras centradas con numeración automática y leyendas.
   - Enlaces internos convertidos a marcadores Word con referencias de página.
   - Tabla de preguntas y problemas frecuentes.
   - Portada corporativa con logo (`assets/logo-encabezado.png`), índice automático y glosario de términos.

2. **`finalize.ps1`**:
   - Utiliza Microsoft Word mediante `Word.Application` COM.
   - Realiza dos pasadas de repaginación para garantizar la exactitud de los números de página en el índice analítico.
   - Exporta el PDF con marcadores estructurales y compatibilidad nativa.

---

> [!NOTE]
> Esta herramienta es **100% autónoma y local**. No utiliza ni requiere Microsoft Graph API, credenciales en la nube ni servicios externos para la conversión o generación de documentos.
