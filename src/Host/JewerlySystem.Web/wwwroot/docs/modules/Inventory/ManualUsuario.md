# 💎 Manual de Usuario — Módulo de Inventario y Catálogo
<!-- manual: codigo=POMONA.INV; version=1.0 -->

Bienvenido al manual oficial de usuario para la administración del inventario, vitrina de joyas y alta relojería en **JewelrySystem** (Joyería Pomona).

---

<details id="proceso-1-catalogo">
<summary><h2 class="doc-process-title">💎 Proceso 1: Consulta y Filtrado del Catálogo de Vitrina</h2></summary>
<!-- fuente-ui: src/Modules/Inventory/JewerlySystem.Modules.Inventory.Presentation/Pages/InventoryList.razor -->

<br/>

### 🎯 Objetivo del Proceso
Facilitar la localización ágil de piezas de joyería y relojería disponibles en el stock activo de la vitrina, permitiendo aplicar búsquedas textuales, filtros por referencia o tipo, y el monitoreo de métricas de valuación en tiempo real.

> 👥 **Roles autorizados:** Administrador de vitrina, Cajero, Tasador, Auditor de inventario.  
> 🔑 **Prerrequisitos:** Usuario autenticado con permisos de visualización sobre el módulo de inventario.

#### ¿Qué necesita hacer?
* [Consultar el catálogo de joyas](#paso-1-1-joyeria)
* [Cambiar a la vista de relojería](#paso-1-2-relojeria)
* [Buscar por referencia, tipo o serial](#paso-1-3-busqueda)

<div id="paso-1-1-joyeria"></div>

### 📌 Paso 1: Exploración del Catálogo de Joyas
1. Ingrese desde el menú principal a **"Inventario > Catálogo"** (o navegue a `/inventory`).
2. El sistema muestra por defecto la pestaña **"Joyería"**, acompañada de las tarjetas métricas de consolidado:
   - **Joyas Disponibles:** Cantidad neta de piezas activas en stock.
   - **Peso Total en Stock:** Sumatoria en gramos (g) de metales preciosos en custodia.
   - **Inversión Costo Joyas:** Capital total invertido a precio de costo.
   - **Venta Sugerida Total:** Proyección de valor comercial esperado.

> 💡 **Nota:** Cada fila de la tabla expone la referencia (#), tipo de joya, pesaje en gramos, valor por gramo, costo, precio de venta sugerido y el estado actual de la pieza.

<div id="paso-1-2-relojeria"></div>

### 📌 Paso 2: Conmutación al Catálogo de Relojes
1. En la barra superior del encabezado (**SoftHeroHeader**), haga clic sobre la opción **"Relojes"** del selector de grupos.
2. La vista se actualizará inmediatamente, mostrando las métricas especializadas de relojería:
   - Total de relojes disponibles.
   - Inversión en costo de relojes.
   - Valor comercial sugerido total.
3. La grilla desplegará el número de referencia, la marca (Rolex, Omega, Cartier, etc.), el género (Caballero, Dama, Unisex), el número de caja grabado y la descripción física.

<div id="paso-1-3-busqueda"></div>

### 📌 Paso 3: Búsqueda y Filtrado Inmediato
1. En la caja de búsqueda superior, digite cualquier término clave:
   - Si escribe un número precedido de numeral o directo (ej. `1045`), el sistema filtrará por la **referencia exacta**.
   - Si escribe un texto (ej. `cadena`, `anillo`, `diamante`, `oro blanco`), el sistema filtrará por el tipo o las palabras de la descripción.
2. Para reiniciar los filtros, haga clic en el botón de limpieza **"X"** dentro del campo de búsqueda.

### ❓ Problemas Frecuentes

**¿Por qué un artículo recién registrado no aparece en el listado inicial?**  
Verifique si tiene activo el filtro de búsqueda con algún término previo o si la pieza fue marcada como inactiva. Al limpiar el buscador, la pieza se listará de inmediato.

</details>

---

<details id="proceso-2-registro">
<summary><h2 class="doc-process-title">💍 Proceso 2: Registro de Nuevos Artículos en Catálogo</h2></summary>
<!-- fuente-ui: src/Modules/Inventory/JewerlySystem.Modules.Inventory.Presentation/Pages/InventoryList.razor -->

<br/>

### 🎯 Objetivo del Proceso
Ingresar nuevas joyas de metales preciosos o relojes de alta gama al inventario comercial, asegurando la asignación atómica de referencia, el registro de pesajes exactos y la trazabilidad de auditoría.

> 👥 **Roles autorizados:** Administrador de vitrina, Tasador principal.  
> 🔑 **Prerrequisitos:** Disponer de los datos físicos pesados en báscula calibrada o los seriales de caja verificados.

#### ¿Qué necesita hacer?
* [Registrar una nueva joya](#paso-2-1-nueva-joya)
* [Registrar un nuevo reloj](#paso-2-2-nuevo-reloj)

<div id="paso-2-1-nueva-joya"></div>

### 📌 Paso 1: Registro de una Joya
1. Haga clic en el botón **"+ Nueva Joya"** en la barra de acciones de la página.
2. Se desplegará el panel lateral (**SoftDrawer**):
   - **Tipo de Joya:** Seleccione la clasificación (Anillo, Cadena, Pulsera, Dije, Aretes).
   - **Peso en Gramos (g):** Digite el peso neto de la pieza con precisión decimal (ej. `7.45`).
   - **Valor por Gramo ($):** Ingrese la tasa de valoración por gramo. El sistema calculará automáticamente el costo base sugerido.
   - **Valor de Costo ($):** Costo total de adquisición de la pieza.
   - **Longitud (cm):** Opcional para cadenas y pulseras.
   - **Descripción Detallada:** Describa el diseño, kilataje (18k), estado de conservación o pedrería incrustada.
3. Presione el botón **"Guardar Joya"**.
4. El sistema asignará automáticamente la siguiente referencia correlativa disponible y registrará el evento de creación en la bitácora de auditoría.

> 🔒 **Seguridad y Auditoría:** Al guardar la joya, el sistema almacena de forma inmutable el usuario autenticado que efectuó el registro y la marca de tiempo UTC exacta.

<div id="paso-2-2-nuevo-reloj"></div>

### 📌 Paso 2: Registro de un Reloj
1. En el catálogo o conmutador de relojes, haga clic en **"+ Nuevo Reloj"**.
2. Complete los campos requeridos en el panel lateral:
   - **Marca:** Seleccione la marca del catálogo oficial de fabricantes.
   - **Género:** Indique si la pieza es para Caballero, Dama o Unisex.
   - **Número de Caja / Serial:** Digite el número alfanumérico grabado en la tapa posterior.
   - **Valor de Costo ($):** Ingrese el costo de ingreso del reloj.
   - **Descripción:** Indique modelo, materiales (acero, oro, cuero), color de esfera y estado funcional.
3. Presione **"Guardar Reloj"**.

### ❓ Problemas Frecuentes

**¿Qué ocurre si el peso ingresado es cero o negativo?**  
El sistema cuenta con validaciones automáticas mediante FluentValidation; le alertará con un mensaje en pantalla impidiendo guardar valores de gramaje o costo menores o iguales a cero.

</details>

---

<details id="proceso-3-edicion">
<summary><h2 class="doc-process-title">✏️ Proceso 3: Edición y Modificación de Especificaciones</h2></summary>
<!-- fuente-ui: src/Modules/Inventory/JewerlySystem.Modules.Inventory.Presentation/Pages/InventoryList.razor -->

<br/>

### 🎯 Objetivo del Proceso
Permitir la actualización controlada de las características técnicas, corrección de descripciones o reajuste de avalúos y costos sobre piezas existentes en vitrina.

> 👥 **Roles autorizados:** Administrador de vitrina, Tasador autorizado.  
> 🔑 **Prerrequisitos:** Localizar la referencia a modificar.

#### ¿Qué necesita hacer?
* [Editar una joya existente](#paso-3-1-editar-joya)
* [Editar un reloj existente](#paso-3-2-editar-reloj)

<div id="paso-3-1-editar-joya"></div>

### 📌 Paso 1: Edición de Joya
1. En la fila del artículo deseado, ubique la columna **"Acciones"** y haga clic en el botón con icono de lápiz (**"Editar Joya"**).
2. Se abrirá el panel lateral de edición con los datos precargados.
3. Modifique el campo necesario (descripción, gramaje, valor por gramo o costo).
4. Haga clic en **"Guardar Cambios"**.
5. El sistema persistirá la modificación, actualizará los campos de auditoría `LastModifiedAt` y `LastModifiedBy`, y registrará el evento de actualización con su responsable en la bitácora.

<div id="paso-3-2-editar-reloj"></div>

### 📌 Paso 2: Edición de Reloj
1. En la fila del reloj, haga clic en el botón de lápiz (**"Editar Reloj"**).
2. Ajuste la marca, número de caja, género o descripción física.
3. Guarde los cambios para confirmar la actualización.

### ❓ Problemas Frecuentes

**¿Puedo cambiar el número de referencia asignado a una pieza?**  
No. El número de referencia es inmutable para preservar la trazabilidad fiscal, los contratos asociados y los registros de auditoría histórica.

</details>

---

<details id="proceso-4-historial">
<summary><h2 class="doc-process-title">🕒 Proceso 4: Consulta de Historial y Auditoría del Artículo</h2></summary>
<!-- fuente-ui: src/Modules/Inventory/JewerlySystem.Modules.Inventory.Presentation/Components/ItemAuditHistoryPanel.razor -->

<br/>

### 🎯 Objetivo del Proceso
Consultar de manera transparente y cronológica la trazabilidad completa de cualquier artículo del inventario, conociendo con exactitud **quién**, **cuándo** y **qué operación** se realizó sobre la pieza desde su momento de creación hasta la fecha actual.

> 👥 **Roles autorizados:** Auditores internos, Supervisores, Tasadores y Administradores.  
> 🔑 **Prerrequisitos:** Conocer el número de referencia del artículo o localizarlo en la tabla del catálogo.

#### ¿Qué necesita hacer?
* [Abrir el panel de historial desde la grilla](#paso-4-1-abrir-historial)
* [Consultar el historial desde la vista de detalle](#paso-4-2-detalle-historial)
* [Interpretar los eventos de la línea de tiempo](#paso-4-3-eventos-timeline)

<div id="paso-4-1-abrir-historial"></div>

### 📌 Paso 1: Apertura del Historial desde la Tabla
1. Ubique la fila de la joya o reloj en la tabla de inventario.
2. En la columna **"Acciones"**, haga clic sobre el primer botón con icono de reloj (**"Historial del Artículo"**).
3. Se desplegará desde el costado derecho el panel interactivo **"Historial y Auditoría — Ref #XXXX"**.

> 💡 **Nota:** Esta acción está disponible tanto en el catálogo de Joyería como en el catálogo de Relojes, e incluso en artículos que ya han sido vendidos o descargados.

<div id="paso-4-2-detalle-historial"></div>

### 📌 Paso 2: Acceso desde el Visor de Detalle
1. Si abrió la vista de detalle haciendo clic en el icono de ojo (**"Ver Detalle"**), encontrará en el pie de página del panel el botón secundario **"Historial"** (con icono de reloj).
2. Al presionarlo, el sistema abrirá la trazabilidad completa del artículo inspeccionado.

<div id="paso-4-3-eventos-timeline"></div>

### 📌 Paso 3: Interpretación de la Línea de Tiempo de Operaciones
El panel despliega dos secciones clave:

1. **Tarjeta de Resumen y Estado:**
   - Tipo de artículo y referencia destacada.
   - Etiqueta de estado (Activo en Catálogo, Inactivo o Descargado / Vendido).
   - Gramaje o número de caja según corresponda.
   - **Creado por:** Nombre del funcionario que originó el registro y fecha/hora exacta.
   - **Última Edición:** Funcionario y fecha/hora de la modificación más reciente.

2. **Línea de Tiempo Cronológica (Timeline):**
   Cada nodo de la línea representa una intervención real sobre el artículo, organizada en orden descendente (lo más reciente al principio):
   - **Etiqueta de Operación:** Con código de color distintivo:
     - 🟦 **Creación de Joya / Creación de Reloj:** Creación inicial en el sistema con sus datos de tasación base.
     - 🟪 **Actualización de Joya / Actualización de Reloj:** Edición de gramaje, valores o descripción técnica.
     - 🟩 **Auditoría de Catálogo:** Activación de la pieza para vitrina.
     - 🟧 **Auditoría de Catálogo (Desactivación):** Puesta en bodega o retiro temporal.
     - 🟥 **Descargo por Venta:** Salida comercial con indicación del valor y fecha de transacción.
   - **Fecha y Hora:** Marca de tiempo exacta con segundos (`dd/MM/yyyy HH:mm:ss`).
   - **Funcionario Responsable:** Usuario autenticado que ejecutó la acción.
   - **Detalle de la Operación:** Descripción detallada de lo que se ejecutó y los valores involucrados.

### ❓ Problemas Frecuentes

**¿Qué ocurre si un artículo fue creado antes de la implementación de la bitácora electrónica?**  
El sistema sintetiza y reconstruye automáticamente el evento de creación inicial a partir de las marcas inmutables de base de datos (`CreatedAt` / `DateOfEntry` y `CreatedBy`), garantizando que ningún artículo quede sin trazabilidad de origen.

</details>

---

<details id="proceso-5-descargo">
<summary><h2 class="doc-process-title">🏷️ Proceso 5: Descargo y Salida por Venta de Artículos</h2></summary>
<!-- fuente-ui: src/Modules/Inventory/JewerlySystem.Modules.Inventory.Presentation/Pages/DischargePage.razor -->

<br/>

### 🎯 Objetivo del Proceso
Registrar la salida definitiva de un artículo del inventario activo debido a su venta comercial o facturación, capturando la fecha de la venta y el valor real recaudado.

> 👥 **Roles autorizados:** Cajero, Administrador de ventas.  
> 🔑 **Prerrequisitos:** La pieza debe encontrarse en estado Activo en el catálogo.

#### ¿Qué necesita hacer?
* [Descargar un artículo desde la tabla](#paso-5-1-descargo-directo)

<div id="paso-5-1-descargo-directo"></div>

### 📌 Paso 1: Registro de Descargo por Venta
1. En la grilla de artículos, sobre la fila de la pieza vendida, haga clic en el botón de salida (**"Descargar / Vender"**).
2. El sistema abrirá la pantalla de confirmación de descargo con los datos de la referencia preseleccionada.
3. Verifique la fecha de venta (por defecto la fecha actual).
4. Digite el **Valor de Venta Real ($)** pactado con el cliente.
5. Confirme la operación presionando **"Confirmar Descargo"**.
6. El estado del artículo cambiará inmediatamente a **Descargado**, la pieza saldrá del stock disponible y se generará el evento correspondiente en el historial de auditoría.

### ❓ Problemas Frecuentes

**¿Un artículo descargado se elimina físicamente de la base de datos?**  
No. Bajo las políticas de integridad del sistema, los artículos nunca se eliminan físicamente. Mantienen su historial, datos de costo y valor de venta para auditorías contables y estadísticas.

</details>

---

<details id="proceso-6-auditoria-rapida">
<summary><h2 class="doc-process-title">🔍 Proceso 6: Auditoría Rápida y Conmutación de Estados</h2></summary>
<!-- fuente-ui: src/Modules/Inventory/JewerlySystem.Modules.Inventory.Presentation/Pages/InventoryList.razor -->

<br/>

### 🎯 Objetivo del Proceso
Facilitar el control periódico de inventario físico en vitrina mediante la activación o desactivación rápida de piezas sin necesidad de ingresar a formularios complejos.

> 👥 **Roles autorizados:** Auditores de inventario, Administradores.

<div id="paso-6-1-conmutacion"></div>

### 📌 Paso 1: Cambio de Estado en Catálogo
1. En la tabla de catálogo, localice la columna **"Estado"**.
2. Cada artículo no vendido presenta un interruptor (**Switch**) o selector de estado.
3. Al hacer clic sobre el interruptor, el sistema conmuta inmediatamente entre **Activo** e **Inactivo**.
4. Cada cambio genera automáticamente un registro de auditoría rápida en la bitácora con peso ponderado para el usuario responsable.

</details>
