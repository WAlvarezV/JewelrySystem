using JewerlySystem.Common.Application.Voice;
using JewerlySystem.Common.Domain;
using JewerlySystem.Modules.Cash.Application.Commands;
using JewerlySystem.Modules.Cash.Domain;
using JewerlySystem.Modules.Contracts.Application.Commands;
using JewerlySystem.Modules.Contracts.Domain;
using JewerlySystem.Modules.Customers.Application;
using JewerlySystem.Modules.Customers.Application.Commands;
using JewerlySystem.Modules.Inventory.Application;
using JewerlySystem.Modules.Inventory.Application.Commands;
using JewerlySystem.Modules.Invoicing.Application.Commands;
using MediatR;
using Microsoft.Extensions.Logging;
using CashPaymentMethod = JewerlySystem.Modules.Cash.Domain.PaymentMethod;
using ContractPaymentMethod = JewerlySystem.Modules.Contracts.Domain.PaymentMethod;

namespace JewerlySystem.Web.Services.Voice;

public class VoiceCommandDispatcher : IVoiceCommandDispatcher
{
    private readonly ISender _sender;
    private readonly ICustomerRepository _customerRepository;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly ILogger<VoiceCommandDispatcher> _logger;

    public VoiceCommandDispatcher(
        ISender sender,
        ICustomerRepository customerRepository,
        IInventoryRepository inventoryRepository,
        ILogger<VoiceCommandDispatcher> logger)
    {
        _sender = sender;
        _customerRepository = customerRepository;
        _inventoryRepository = inventoryRepository;
        _logger = logger;
    }

    public async Task<VoiceExecutionResult> ExecuteAsync(VoiceOperationResult operation, CancellationToken ct = default)
    {
        if (operation == null || string.IsNullOrWhiteSpace(operation.Intent))
        {
            return VoiceExecutionResult.Fail("Operación de voz vacía o no reconocida.");
        }

        if (operation.IsFromExample)
        {
            return VoiceExecutionResult.Fail("Los ejemplos de demostración son sólo ilustrativos y no se pueden guardar en la base de datos.");
        }

        try
        {
            return operation.Intent switch
            {
                VoiceIntents.CrearVenta => await ExecuteSaleAsync(operation.Sale, ct),
                VoiceIntents.CrearContrato => await ExecuteContractAsync(operation.Contract, ct),
                VoiceIntents.CrearCliente => await ExecuteCustomerAsync(operation.Customer, ct),
                VoiceIntents.CrearMovimientoCaja => await ExecuteCashAsync(operation.Cash, ct),
                VoiceIntents.CrearFactura => await ExecuteInvoiceAsync(operation.Invoice, ct),
                _ => VoiceExecutionResult.Fail($"La intención '{operation.Intent}' no tiene un ejecutor configurado.")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ejecutando operación de voz para intención: {Intent}", operation.Intent);
            return VoiceExecutionResult.Fail($"Error al persistir la operación en la base de datos: {ex.Message}");
        }
    }

    private async Task<VoiceExecutionResult> ExecuteSaleAsync(VoiceSalePayload? data, CancellationToken ct)
    {
        if (data == null) return VoiceExecutionResult.Fail("No se encontraron datos de la venta.");

        // 1. REGLA ESTRICTA: El número de referencia es obligatorio para descargar inventario
        if (data.Reference <= 0)
        {
            return VoiceExecutionResult.Fail("El número de referencia es obligatorio para poder descargar el artículo del inventario.");
        }

        if (data.SaleValue <= 0)
        {
            return VoiceExecutionResult.Fail("El valor de venta debe ser mayor a 0.");
        }

        // 2. Buscar el artículo en el inventario activo
        var item = await _inventoryRepository.GetItemByReferenceAsync(data.Reference, ct);
        if (item == null)
        {
            return VoiceExecutionResult.Fail($"No se encontró ningún artículo en el catálogo de inventario con la referencia #{data.Reference}.");
        }

        if (!item.Active)
        {
            return VoiceExecutionResult.Fail($"El artículo con referencia #{data.Reference} ya se encuentra descargado/vendido (Fecha: {item.DateOfSale:dd/MM/yyyy}).");
        }

        // 3. Ejecutar descarga de inventario
        var dischargeCmd = new DischargeItemCommand(
            ItemId: item.Id,
            SaleDate: DateTime.Today,
            SaleValue: data.SaleValue
        );

        var dischargeResult = await _sender.Send(dischargeCmd, ct);
        if (!dischargeResult.IsSuccess)
        {
            return VoiceExecutionResult.Fail($"Error al descargar inventario: {dischargeResult.Error.Message}");
        }

        // 4. Registrar automáticamente el ingreso en caja por la venta
        var cashPaymentMethod = ParseCashPaymentMethod(data.PaymentMethod);
        string buyerPart = !string.IsNullOrWhiteSpace(data.CustomerName)
            ? $" (Comprador: {data.CustomerName}" + (!string.IsNullOrWhiteSpace(data.CustomerIdentification) ? $" C.C. {data.CustomerIdentification}" : "") + ")"
            : "";

        string desc = string.IsNullOrWhiteSpace(data.Description)
            ? $"Venta y descarga ref #{data.Reference} - {item.Description}{buyerPart}"
            : $"{data.Description}{buyerPart}";

        var cashCmd = new CreateDailyRecordCommand(
            Date: DateTime.Today,
            Reference: data.Reference,
            Description: desc,
            Value: data.SaleValue,
            RecordType: RecordType.INGRESO,
            PaymentMethod: cashPaymentMethod
        );

        await _sender.Send(cashCmd, ct);

        return VoiceExecutionResult.Ok(
            item.Id,
            "Venta / Inventario",
            $"¡Artículo ref #{data.Reference} ('{item.Description}') descargado exitosamente por venta de ${data.SaleValue:N0} en {cashPaymentMethod}!",
            "/inventory/discharge"
        );
    }

    private async Task<VoiceExecutionResult> ExecuteContractAsync(VoiceContractPayload? data, CancellationToken ct)
    {
        if (data == null) return VoiceExecutionResult.Fail("No se encontraron datos de contrato.");

        if (data.Value <= 0)
        {
            return VoiceExecutionResult.Fail("El valor total del contrato debe ser mayor a 0.");
        }

        // 1. Resolver el cliente (buscar existente o crear)
        int personId = 0;
        string customerName = data.CustomerName?.Trim() ?? "Cliente Mostrador";

        if (!string.IsNullOrWhiteSpace(data.CustomerName))
        {
            var existing = await _customerRepository.ListAsync(searchTerm: data.CustomerName, ct: ct);
            var match = existing.FirstOrDefault();
            if (match != null)
            {
                personId = match.Id;
            }
        }

        if (personId == 0)
        {
            // Auto-crear cliente básico para no trabar el flujo de voz
            var idTypes = await _customerRepository.GetIdentificationTypesAsync(ct);
            int defaultTypeId = idTypes.FirstOrDefault()?.Id ?? 1;
            string docNum = !string.IsNullOrWhiteSpace(data.CustomerIdentification)
                ? data.CustomerIdentification
                : "10" + Random.Shared.Next(10000000, 99999999);

            var createCustomerCmd = new CreateCustomerCommand(
                IdentificationTypeId: defaultTypeId,
                IdentificationNumber: docNum,
                FullName: customerName,
                CellPhone: "3000000000",
                Email: ""
            );

            var customerRes = await _sender.Send(createCustomerCmd, ct);
            if (customerRes.IsSuccess)
            {
                personId = customerRes.Value;
            }
            else
            {
                // Si ya existía por documento, intentar recuperar el primero
                var list = await _customerRepository.ListAsync(ct: ct);
                personId = list.FirstOrDefault()?.Id ?? 1;
            }
        }

        // 2. Mapear método de pago
        var paymentMethod = ParsePaymentMethod(data.PaymentMethod);

        // 3. Crear comando de contrato
        var deliveryDate = DateTime.Today.AddDays(data.DeliveryDays.GetValueOrDefault(15));
        string desc = string.IsNullOrWhiteSpace(data.Description)
            ? $"Hechura/reparación de joya acordada por voz para {customerName}"
            : data.Description;

        var cmd = new CreateContractCommand(
            Reference: null,
            Date: DateTime.Today,
            DeliveryDate: deliveryDate,
            Value: data.Value,
            Weight: data.Weight,
            Description: desc,
            PersonId: personId,
            InitialPayment: data.InitialPayment.GetValueOrDefault(0),
            PaymentMethod: paymentMethod
        );

        var result = await _sender.Send(cmd, ct);
        if (result.IsSuccess)
        {
            return VoiceExecutionResult.Ok(
                result.Value,
                "Contrato",
                $"Contrato #{result.Value} guardado exitosamente para '{customerName}' por ${data.Value:N0} (Abono: ${data.InitialPayment.GetValueOrDefault(0):N0}).",
                "/contracts"
            );
        }

        return VoiceExecutionResult.Fail(result.Error.Message);
    }

    private async Task<VoiceExecutionResult> ExecuteCustomerAsync(VoiceCustomerPayload? data, CancellationToken ct)
    {
        if (data == null) return VoiceExecutionResult.Fail("No se encontraron datos del cliente.");
        if (string.IsNullOrWhiteSpace(data.FullName)) return VoiceExecutionResult.Fail("El nombre del cliente es obligatorio.");

        var idTypes = await _customerRepository.GetIdentificationTypesAsync(ct);
        int typeId = idTypes.FirstOrDefault()?.Id ?? 1;

        string docNumber = !string.IsNullOrWhiteSpace(data.IdentificationNumber)
            ? data.IdentificationNumber
            : "10" + Random.Shared.Next(10000000, 99999999);

        var cmd = new CreateCustomerCommand(
            IdentificationTypeId: typeId,
            IdentificationNumber: docNumber,
            FullName: data.FullName.Trim(),
            CellPhone: data.CellPhone ?? "3000000000",
            Email: data.Email ?? ""
        );

        var result = await _sender.Send(cmd, ct);
        if (result.IsSuccess)
        {
            return VoiceExecutionResult.Ok(
                result.Value,
                "Cliente",
                $"Cliente '{data.FullName}' registrado con documento #{docNumber}.",
                "/customers"
            );
        }

        return VoiceExecutionResult.Fail(result.Error.Message);
    }

    private async Task<VoiceExecutionResult> ExecuteCashAsync(VoiceCashPayload? data, CancellationToken ct)
    {
        if (data == null) return VoiceExecutionResult.Fail("No se encontraron datos del movimiento de caja.");
        if (data.Value <= 0) return VoiceExecutionResult.Fail("El valor del movimiento debe ser mayor a 0.");

        var isEgreso = string.Equals(data.RecordType, "EGRESO", StringComparison.OrdinalIgnoreCase);
        var recordType = isEgreso ? RecordType.EGRESO : RecordType.INGRESO;
        var paymentMethod = ParseCashPaymentMethod(data.PaymentMethod);

        string desc = string.IsNullOrWhiteSpace(data.Description)
            ? (isEgreso ? "Egreso registrado por comando de voz" : "Venta rápida / Ingreso registrado por comando de voz")
            : data.Description;

        var cmd = new CreateDailyRecordCommand(
            Date: DateTime.Today,
            Reference: null,
            Description: desc,
            Value: data.Value,
            RecordType: recordType,
            PaymentMethod: paymentMethod
        );

        var result = await _sender.Send(cmd, ct);
        if (result.IsSuccess)
        {
            return VoiceExecutionResult.Ok(
                result.Value,
                "Caja",
                $"Movimiento de caja #{result.Value} ({(isEgreso ? "Egreso" : "Ingreso")}) registrado por ${data.Value:N0} en {paymentMethod}.",
                "/cash"
            );
        }

        return VoiceExecutionResult.Fail(result.Error.Message);
    }

    private async Task<VoiceExecutionResult> ExecuteInvoiceAsync(VoiceInvoicePayload? data, CancellationToken ct)
    {
        if (data == null) return VoiceExecutionResult.Fail("No se encontraron datos de la factura.");
        if (data.Value <= 0) return VoiceExecutionResult.Fail("El valor de la factura debe ser mayor a 0.");

        var cmd = new CreateInvoiceCommand(
            Date: DateTime.Today,
            PaymentLimitDate: DateTime.Today.AddDays(30),
            Value: data.Value,
            CreditValue: data.CreditValue,
            ProviderId: data.ProviderId > 0 ? data.ProviderId : 1,
            Description: data.Description ?? "Factura registrada por comando de voz"
        );

        var result = await _sender.Send(cmd, ct);
        if (result.IsSuccess)
        {
            return VoiceExecutionResult.Ok(
                result.Value,
                "Factura",
                $"Factura #{result.Value} creada por ${data.Value:N0} (Abono: ${data.CreditValue:N0}).",
                "/invoicing"
            );
        }

        return VoiceExecutionResult.Fail(result.Error.Message);
    }

    private static ContractPaymentMethod ParsePaymentMethod(string? method)
    {
        if (string.IsNullOrWhiteSpace(method)) return ContractPaymentMethod.EFECTIVO;
        var upper = method.ToUpperInvariant();
        if (upper.Contains("TRANSF")) return ContractPaymentMethod.TRANSFERENCIA;
        if (upper.Contains("DEBIT")) return ContractPaymentMethod.TARJETA_DEBITO;
        if (upper.Contains("TARJET") || upper.Contains("CREDIT")) return ContractPaymentMethod.TARJETA_CREDITO;
        if (upper.Contains("ORO")) return ContractPaymentMethod.ORO;
        return ContractPaymentMethod.EFECTIVO;
    }

    private static CashPaymentMethod ParseCashPaymentMethod(string? method)
    {
        if (string.IsNullOrWhiteSpace(method)) return CashPaymentMethod.EFECTIVO;
        var upper = method.ToUpperInvariant();
        if (upper.Contains("TRANSF")) return CashPaymentMethod.TRANSFERENCIA;
        if (upper.Contains("DEBIT")) return CashPaymentMethod.TARJETA_DEBITO;
        if (upper.Contains("TARJET") || upper.Contains("CREDIT")) return CashPaymentMethod.TARJETA_CREDITO;
        if (upper.Contains("ORO")) return CashPaymentMethod.ORO;
        return CashPaymentMethod.EFECTIVO;
    }
}
