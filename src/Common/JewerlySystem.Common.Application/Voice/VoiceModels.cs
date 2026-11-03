namespace JewerlySystem.Common.Application.Voice;

public static class VoiceIntents
{
    public const string CrearVenta = "CREAR_VENTA";
    public const string CrearContrato = "CREAR_CONTRATO";
    public const string CrearCliente = "CREAR_CLIENTE";
    public const string CrearMovimientoCaja = "CREAR_MOVIMIENTO_CAJA";
    public const string CrearFactura = "CREAR_FACTURA";
    public const string Desconocido = "DESCONOCIDO";
}

public class VoiceSalePayload
{
    public int Reference { get; set; } // Número de referencia obligatorio para descargar inventario
    public int SaleValue { get; set; } // Valor de venta acordado
    public string? Description { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerIdentification { get; set; }
    public string? PaymentMethod { get; set; } = "EFECTIVO";
}

public class VoiceContractPayload
{
    public string? CustomerName { get; set; }
    public string? CustomerIdentification { get; set; }
    public string? Description { get; set; }
    public int Value { get; set; }
    public int? InitialPayment { get; set; }
    public double Weight { get; set; }
    public int? DeliveryDays { get; set; } = 15;
    public string? PaymentMethod { get; set; } = "EFECTIVO";
}

public class VoiceCustomerPayload
{
    public string FullName { get; set; } = string.Empty;
    public string? IdentificationNumber { get; set; }
    public string? CellPhone { get; set; }
    public string? Email { get; set; }
}

public class VoiceCashPayload
{
    public string Description { get; set; } = string.Empty;
    public int Value { get; set; }
    public string RecordType { get; set; } = "INGRESO"; // INGRESO o EGRESO
    public string PaymentMethod { get; set; } = "EFECTIVO"; // EFECTIVO, TRANSFERENCIA, TARJETA
}

public class VoiceInvoicePayload
{
    public string? ProviderName { get; set; }
    public int ProviderId { get; set; }
    public int Value { get; set; }
    public int CreditValue { get; set; }
    public string? Description { get; set; }
    public string? PaymentMethod { get; set; } = "EFECTIVO";
}

public class VoiceOperationResult
{
    public string Intent { get; set; } = VoiceIntents.Desconocido;
    public double Confidence { get; set; } = 1.0;
    public string Summary { get; set; } = string.Empty;
    public string RawTranscription { get; set; } = string.Empty;
    public List<string> MissingFields { get; set; } = new();
    
    public VoiceContractPayload? Contract { get; set; }
    public VoiceCustomerPayload? Customer { get; set; }
    public VoiceCashPayload? Cash { get; set; }
    public VoiceInvoicePayload? Invoice { get; set; }
    public VoiceSalePayload? Sale { get; set; }
    
    public string? RawJson { get; set; }
    public bool IsFromExample { get; set; }
    public string? EngineNotice { get; set; }
    public bool IsSuccess => Intent != VoiceIntents.Desconocido && MissingFields.Count == 0 && !IsFromExample;
}

public class VoiceExecutionResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int? EntityId { get; set; }
    public string? EntityType { get; set; }
    public string? RedirectUrl { get; set; }
    public List<string> Errors { get; set; } = new();

    public static VoiceExecutionResult Ok(int id, string type, string message, string? redirectUrl = null) =>
        new() { Success = true, EntityId = id, EntityType = type, Message = message, RedirectUrl = redirectUrl };

    public static VoiceExecutionResult Fail(string error) =>
        new() { Success = false, Message = error, Errors = new List<string> { error } };
}

public interface IVoiceInterpreterService
{
    Task<VoiceOperationResult> InterpretAsync(string transcription, CancellationToken ct = default);
}

public interface IVoiceCommandDispatcher
{
    Task<VoiceExecutionResult> ExecuteAsync(VoiceOperationResult operation, CancellationToken ct = default);
}
