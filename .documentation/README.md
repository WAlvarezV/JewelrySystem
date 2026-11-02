# Documentación del Sistema JewerlySystem

Bienvenido a la documentación técnica y operativa de la solución **JewerlySystem** (.NET 10 Modular Monolith con PostgreSQL).

## Índice de Documentación

1. [Base de Datos PostgreSQL (`IntZaioNJewerly`)](./DATABASE_POSTGRESQL.md)
   * Configuración del motor PostgreSQL 18.
   * Cadena de conexión y parámetros de seguridad.
   * Estructura del esquema, tablas y secuencias autoincrementales.
   * Convención de DateTime y timestamps en Npgsql.

2. [Guía de Migración Repetible de BACPACs](./DATA_MIGRATION_GUIDE.md)
   * Procedimiento paso a paso para importar nuevos archivos `.bacpac` de SQL Server en el futuro.
   * Uso del script automatizado `Migrate-ToPostgreSql.ps1` y la utilidad `DataMigrator`.
   * Reajuste automático de secuencias (`SERIAL/IDENTITY`).
   * Matriz de verificación y paridad de registros.

---

## Resumen de Arquitectura

* **Framework:** .NET 10 (C# 13)
* **Estilo de Arquitectura:** Monolito Modular (Modular Monolith)
* **Motor de Base de Datos:** PostgreSQL 18.4 (x86_64)
* **Proveedor ORM:** `Npgsql.EntityFrameworkCore.PostgreSQL` v10.0.3
* **Gestión Centralizada de Paquetes:** `Directory.Packages.props`
* **Módulos del Sistema:**
  * **Auth:** Identidad, roles, matriz de permisos y Passkeys (FIDO2 / WebAuthn).
  * **Contracts:** Compraventa y empeño de joyas y relojería.
  * **Inventory:** Catálogo y stock de joyas, relojes, marcas y tipos.
  * **Customers:** Personas naturales, jurídicas y tipos de documento.
  * **Cash:** Arqueos de caja diaria y movimientos contables.
  * **Invoicing:** Facturas de venta y control de pagos.
