// Contenido propio de la versión de capacitación del módulo Inventario y Artículos.
// El texto de los procesos NO va aquí: se toma de ManualUsuario.md. Aquí solo va lo que el .md no tiene.
module.exports = {
  // Ruta del manual en línea, relativa a la raíz del repositorio. Las imágenes se resuelven desde su carpeta.
  manualPath: 'src/Host/JewerlySystem.Web/wwwroot/docs/modules/Inventory/ManualUsuario.md',
  outputName: 'Manual de Usuario Inventario - Capacitacion',

  // Encabezado institucional: (logo | título | código y versión).
  header: { 
    title: 'DOCUMENTACIÓN DE SISTEMAS — JOYERÍA POMONA', 
    code: 'POMONA.INV', 
    version: 'Ver. 1.0' 
  },
  footerText: 'Manual de Usuario — Módulo Inventario y Artículos · Versión de capacitación',

  cover: {
    title: 'Manual de Usuario',
    subtitle: 'Módulo Inventario y Artículos',
    tagline: 'Gestión Integral de Joyería, Relojería, Pesajes, Trazabilidad e Historial',
    info: [
      ['Sistema', 'JewelrySystem — Joyería Pomona (.NET 10 Modular Monolith)'],
      ['Módulo', 'Inventario (Joyería y Relojes) — Catálogo, Trazabilidad e Historial'],
      ['Dirigido a', 'Administradores de vitrina, cajeros, tasadores y auditores de catálogo'],
      ['Uso', 'Material oficial de apoyo para capacitación y operación'],
      ['Fecha', 'Noviembre de 2026'],
    ],
  },

  intro: 'Este manual acompaña la capacitación en el módulo de Inventario y Catálogo de Artículos de JewelrySystem. Describe detalladamente cómo consultar existencias, registrar nuevas piezas de oro y relojería fina, editar especificaciones técnicas, auditar el historial cronológico de cambios (quién, cuándo y qué operación se realizó) y gestionar la salida o descargo por venta.',

  vistas: [],

  // Mapa de procesos de "Cómo usar este manual". La clave es el número de proceso (orden de los <details> en el .md).
  processMap: {
    1: { who: 'Cajeros, tasadores, administradores', where: 'Menú lateral **"Inventario > Catálogo"** (/inventory)' },
    2: { who: 'Tasadores y administradores de vitrina', where: 'Botones **"+ Nueva Joya"** y **"+ Nuevo Reloj"**' },
    3: { who: 'Tasadores y administradores', where: 'Acción **"Editar Joya"** / **"Editar Reloj"**' },
    4: { who: 'Auditores, supervisores y administradores', where: 'Botón **"Historial"** (tabla y panel de detalle)' },
    5: { who: 'Cajeros y personal de ventas', where: 'Acción **"Descargar / Vender"**' },
    6: { who: 'Auditores de inventario físico', where: 'Interruptor **"Activo / Inactivo"**' },
  },

  // "Resumen del proceso": diagrama de pasos que se inserta antes de la tabla "¿Qué necesita hacer?" de cada proceso.
  flows: {
    1: ['Acceso al catálogo', 'Conmutación Joyas / Relojes', 'Búsqueda por texto o ref', 'Inspección de métricas'],
    2: ['Apertura de drawer', 'Datos básicos y tipo', 'Pesaje / Serial de caja', 'Cálculo de costo y margen', 'Guardado atómico'],
    3: ['Búsqueda de referencia', 'Apertura de edición', 'Ajuste de gramaje o descripción', 'Confirmación y auditoría'],
    4: ['Clic en botón Historial', 'Apertura de panel lateral', 'Revisión de creación original', 'Inspección de intervenciones', 'Trazabilidad de responsable'],
    5: ['Selección de pieza activa', 'Ingreso fecha y precio venta', 'Confirmación de descargo', 'Actualización de stock'],
    6: ['Revisión de vitrina física', 'Conmutación de estado', 'Registro automático de bitácora', 'Sincronización de catálogo'],
  },

  // Glosario (orden alfabético).
  glossary: [
    ['Activo / Inactivo', 'Estado operativo de un artículo en el catálogo. Un artículo inactivo no está disponible para venta ni en vitrina pero conserva su historial.'],
    ['Bitácora de actividad (UserActivityLog)', 'Registro atómico y permanente que almacena qué usuario realizó una acción, en qué fecha/hora exacta y sobre qué referencia de inventario.'],
    ['Descargo por venta', 'Operación que retira una pieza del stock comercial registrando la fecha de salida y el valor recaudado de la transacción.'],
    ['Gramaje', 'Peso neto en gramos (g) de la joya de metal precioso. Es la base fundamental para el costeo y la valoración de inventario.'],
    ['Historial y auditoría', 'Panel interactivo que despliega la línea de tiempo completa del artículo, desde su creación inicial, ediciones y cambios de estado con fecha y funcionario.'],
    ['Item / Artículo', 'Entidad base del catálogo que posee una referencia única numérica, costo, fecha de entrada, descripción y estado.'],
    ['Joya', 'Subtipo de artículo que extiende las propiedades básicas con peso en gramos, valor por gramo y longitud en centímetros (si aplica).'],
    ['Número de caja', 'Identificador alfanumérico o serial grabado en la tapa trasera de un reloj para autenticación y control de unicidad.'],
    ['Referencia', 'Número entero único asignado a cada pieza en inventario, utilizado para rotulación física de vitrina y búsqueda inmediata.'],
    ['Reloj', 'Subtipo de artículo que incluye marca, número de caja/serial y género (Caballero, Dama, Unisex).'],
    ['Valor por gramo', 'Precio unitario fijado por gramo de metal (oro 18k / plata) al momento del ingreso o retasación de la pieza.'],
  ],
};
