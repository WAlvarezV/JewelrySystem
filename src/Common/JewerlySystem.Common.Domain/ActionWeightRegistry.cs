namespace JewerlySystem.Common.Domain;

public record ActionDefinition(
    string ActionKey,
    string ModuleKey,
    string DisplayName,
    string Category,
    int DefaultWeight,
    string Icon,
    string ColorHex);

/// <summary>
/// Catálogo y ponderaciones oficiales del sistema para cuantificar el esfuerzo operativo de cada acción.
/// </summary>
public static class ActionWeightRegistry
{
    // Categorías oficiales
    public const string CategoryNewRecord = "Nuevo Registro";
    public const string CategoryUpdate = "Actualización";
    public const string CategoryAuditQuick = "Auditoría Rápida";
    public const string CategoryDischarge = "Operación / Venta";
    public const string CategoryCash = "Operación Caja";

    // Keys de Acciones
    public const string InventoryCreateJewel = "Inventory_CreateJewel";
    public const string InventoryCreateWatch = "Inventory_CreateWatch";
    public const string InventoryUpdateJewel = "Inventory_UpdateJewel";
    public const string InventoryUpdateWatch = "Inventory_UpdateWatch";
    public const string InventoryUpdateImage = "Inventory_UpdateImage";
    public const string InventoryToggleActive = "Inventory_ToggleActive";
    public const string InventoryDischarge = "Inventory_Discharge";

    public const string ContractsCreateContract = "Contracts_CreateContract";
    public const string ContractsRegisterPayment = "Contracts_RegisterPayment";
    public const string InvoicingCreateInvoice = "Invoicing_CreateInvoice";
    public const string CustomersCreateCustomer = "Customers_CreateCustomer";
    public const string CustomersUpdateCustomer = "Customers_UpdateCustomer";
    public const string CashDailyMovement = "Cash_DailyMovement";

    public static readonly IReadOnlyDictionary<string, ActionDefinition> Definitions =
        new Dictionary<string, ActionDefinition>(StringComparer.OrdinalIgnoreCase)
        {
            [InventoryCreateJewel] = new(
                InventoryCreateJewel,
                "INVENTORY",
                "Creación de Joya Nueva",
                CategoryNewRecord,
                10,
                "bi-gem",
                "#10B981"), // Esmeralda

            [InventoryCreateWatch] = new(
                InventoryCreateWatch,
                "INVENTORY",
                "Creación de Reloj Nuevo",
                CategoryNewRecord,
                10,
                "bi-watch",
                "#059669"), // Verde oscuro

            [InventoryUpdateJewel] = new(
                InventoryUpdateJewel,
                "INVENTORY",
                "Actualización de Joya",
                CategoryUpdate,
                4,
                "bi-pencil-square",
                "#3B82F6"), // Azul

            [InventoryUpdateWatch] = new(
                InventoryUpdateWatch,
                "INVENTORY",
                "Actualización de Reloj",
                CategoryUpdate,
                4,
                "bi-pencil-square",
                "#2563EB"), // Azul medio

            [InventoryUpdateImage] = new(
                InventoryUpdateImage,
                "INVENTORY",
                "Actualización de Fotografía",
                CategoryUpdate,
                2,
                "bi-camera",
                "#8B5CF6"), // Púrpura

            [InventoryToggleActive] = new(
                InventoryToggleActive,
                "INVENTORY",
                "Activación / Conteo en Lista",
                CategoryAuditQuick,
                1,
                "bi-check2-circle",
                "#F59E0B"), // Dorado/Ámbar

            [InventoryDischarge] = new(
                InventoryDischarge,
                "INVENTORY",
                "Descargo / Venta Inventario",
                CategoryDischarge,
                6,
                "bi-arrow-down-left-square",
                "#EF4444"), // Rojo

            [ContractsCreateContract] = new(
                ContractsCreateContract,
                "CONTRACTS",
                "Creación de Contrato",
                CategoryNewRecord,
                15,
                "bi-file-earmark-text",
                "#8B5CF6"), // Púrpura

            [ContractsRegisterPayment] = new(
                ContractsRegisterPayment,
                "CONTRACTS",
                "Abono / Pago Contrato",
                CategoryCash,
                5,
                "bi-cash",
                "#6366F1"),

            [InvoicingCreateInvoice] = new(
                InvoicingCreateInvoice,
                "INVOICING",
                "Creación de Factura",
                CategoryNewRecord,
                12,
                "bi-receipt",
                "#EC4899"),

            [CustomersCreateCustomer] = new(
                CustomersCreateCustomer,
                "CUSTOMERS",
                "Registro de Cliente",
                CategoryNewRecord,
                5,
                "bi-person-plus",
                "#14B8A6"),

            [CustomersUpdateCustomer] = new(
                CustomersUpdateCustomer,
                "CUSTOMERS",
                "Actualización de Cliente",
                CategoryUpdate,
                2,
                "bi-person-check",
                "#06B6D4"),

            [CashDailyMovement] = new(
                CashDailyMovement,
                "CASH",
                "Movimiento Diario Caja",
                CategoryCash,
                3,
                "bi-wallet2",
                "#D97706")
        };

    public static int GetDefaultWeight(string actionKey)
    {
        return Definitions.TryGetValue(actionKey, out var def) ? def.DefaultWeight : 1;
    }

    public static ActionDefinition? GetDefinition(string actionKey)
    {
        return Definitions.TryGetValue(actionKey, out var def) ? def : null;
    }
}
