-- Seed basic structure from IntZaioNJewerly to IntZaioNJewerly_OroPlata
INSERT INTO "IdentificationTypes" ("Id", "Name")
VALUES 
    (1, 'Cédula de Ciudadanía'),
    (2, 'Cédula de Extranjería'),
    (3, 'NIT'),
    (4, 'Pasaporte')
ON CONFLICT ("Id") DO NOTHING;

-- Seed Navigation Modules and Menus
INSERT INTO "AppModules" ("Id", "Key", "Name", "Icon", "Description", "Order", "IsActive")
VALUES
    ('11111111-1111-1111-1111-111111111111', 'CONTRACTS', 'Contratos', 'bi-file-earmark-text', 'Gestión integral de contratos de empeño y compraventa', 1, true),
    ('22222222-2222-2222-2222-222222222222', 'INVENTORY', 'Inventario', 'bi-gem', 'Catálogo de joyas, relojes y artículos valorados', 2, true),
    ('33333333-3333-3333-3333-333333333333', 'CUSTOMERS', 'Clientes', 'bi-people', 'Registro y consulta de clientes y proveedores', 3, true),
    ('44444444-4444-4444-4444-444444444444', 'CASH', 'Caja', 'bi-cash-stack', 'Arqueo y control diario de caja', 4, true),
    ('55555555-5555-5555-5555-555555555555', 'INVOICING', 'Facturación', 'bi-receipt', 'Emisión y control de facturas', 5, true),
    ('66666666-6666-6666-6666-666666666666', 'ADMIN', 'Seguridad', 'bi-shield-lock', 'Usuarios, módulos, menús, roles y permisos', 6, true)
ON CONFLICT ("Id") DO NOTHING;

INSERT INTO "MenuItems" ("Id", "ModuleKey", "ParentId", "Name", "Route", "Icon", "Order", "IsActive", "RequiredPermission")
VALUES
    ('11111111-1111-1111-1111-111111111112', 'CONTRACTS', NULL, 'Lista de Contratos', '/contracts', 'bi-card-list', 1, true, NULL),
    ('11111111-1111-1111-1111-111111111113', 'CONTRACTS', NULL, 'Nuevo Contrato', '/contracts/new', 'bi-plus-circle', 2, true, NULL),
    ('22222222-2222-2222-2222-222222222223', 'INVENTORY', NULL, 'Stock General', '/inventory', 'bi-box-seam', 1, true, NULL),
    ('22222222-2222-2222-2222-222222222224', 'INVENTORY', NULL, 'Nueva Joya', '/inventory/jewels/new', 'bi-gem', 2, true, NULL),
    ('22222222-2222-2222-2222-222222222225', 'INVENTORY', NULL, 'Nuevo Reloj', '/inventory/watches/new', 'bi-watch', 3, true, NULL),
    ('22222222-2222-2222-2222-222222222226', 'INVENTORY', NULL, 'Descargos', '/inventory/discharge', 'bi-arrow-down-left-square', 4, true, NULL),
    ('22222222-2222-2222-2222-222222222227', 'INVENTORY', NULL, 'Consolidado Stock', '/inventory/consolidated', 'bi-pie-chart', 5, true, NULL),
    ('33333333-3333-3333-3333-333333333334', 'CUSTOMERS', NULL, 'Directorio Clientes', '/customers', 'bi-person-lines-fill', 1, true, NULL),
    ('33333333-3333-3333-3333-333333333335', 'CUSTOMERS', NULL, 'Nuevo Cliente', '/customers/new', 'bi-person-plus', 2, true, NULL),
    ('44444444-4444-4444-4444-444444444445', 'CASH', NULL, 'Movimiento Diario', '/cash/daily', 'bi-calendar-check', 1, true, NULL),
    ('44444444-4444-4444-4444-444444444446', 'CASH', NULL, 'Consolidado Histórico', '/cash/consolidated', 'bi-graph-up', 2, true, NULL),
    ('55555555-5555-5555-5555-555555555556', 'INVOICING', NULL, 'Lista Facturas', '/invoices', 'bi-receipt-cutoff', 1, true, NULL),
    ('55555555-5555-5555-5555-555555555557', 'INVOICING', NULL, 'Nueva Factura', '/invoices/new', 'bi-plus-circle', 2, true, NULL),
    ('66666666-6666-6666-6666-666666666667', 'ADMIN', NULL, 'Usuarios', '/admin/users', 'bi-people-fill', 1, true, NULL),
    ('66666666-6666-6666-6666-666666666668', 'ADMIN', NULL, 'Roles y Perfiles', '/admin/roles', 'bi-award-fill', 2, true, NULL),
    ('66666666-6666-6666-6666-666666666669', 'ADMIN', NULL, 'Matriz de Permisos', '/admin/permissions', 'bi-key-fill', 3, true, NULL),
    ('66666666-6666-6666-6666-666666666670', 'ADMIN', NULL, 'Módulos del Sistema', '/admin/modules', 'bi-grid-fill', 4, true, NULL),
    ('66666666-6666-6666-6666-666666666671', 'ADMIN', NULL, 'Menús de Navegación', '/admin/menus', 'bi-menu-button-wide-fill', 5, true, NULL)
ON CONFLICT ("Id") DO NOTHING;

-- Seed Roles
INSERT INTO "AspNetRoles" ("Id", "Name", "NormalizedName", "ConcurrencyStamp")
VALUES
    ('10000000-0000-0000-0000-000000000001', 'Administrador', 'ADMINISTRADOR', '10000000-0000-0000-0000-000000000001'),
    ('10000000-0000-0000-0000-000000000002', 'Admin', 'ADMIN', '10000000-0000-0000-0000-000000000002'),
    ('10000000-0000-0000-0000-000000000003', 'Supervisor', 'SUPERVISOR', '10000000-0000-0000-0000-000000000003'),
    ('10000000-0000-0000-0000-000000000004', 'Tasador', 'TASADOR', '10000000-0000-0000-0000-000000000004'),
    ('10000000-0000-0000-0000-000000000005', 'Cajero', 'CAJERO', '10000000-0000-0000-0000-000000000005'),
    ('10000000-0000-0000-0000-000000000006', 'Vendedor', 'VENDEDOR', '10000000-0000-0000-0000-000000000006')
ON CONFLICT ("Id") DO NOTHING;
