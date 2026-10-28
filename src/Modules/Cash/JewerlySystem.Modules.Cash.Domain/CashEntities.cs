using JewerlySystem.Common.Domain;

namespace JewerlySystem.Modules.Cash.Domain;

public enum RecordType
{
    INGRESO = 1,
    EGRESO = 2
}

public enum PaymentMethod
{
    EFECTIVO = 1,
    TARJETA_CREDITO = 2,
    TARJETA_DEBITO = 3,
    TRANSFERENCIA = 4,
    ORO = 5
}

public class DailyRecord : AuditableEntity<int>
{
    public DateTime Date { get; set; }
    public string Number { get; set; } = string.Empty;
    public int? Reference { get; set; }
    public string Description { get; set; } = string.Empty;
    public int Value { get; set; }
    public RecordType RecordType { get; set; }
    public PaymentMethod PaymentMethod { get; set; }

    public DailyRecord() { }

    public DailyRecord(
        DateTime date,
        string number,
        int? reference,
        string description,
        int value,
        RecordType recordType,
        PaymentMethod paymentMethod)
    {
        Date = date;
        Number = number ?? string.Empty;
        Reference = reference;
        Description = description ?? string.Empty;
        Value = value;
        RecordType = recordType;
        PaymentMethod = paymentMethod;
    }
}

public class ConsolidatedRecord
{
    public DateTime Date { get; set; }
    public int TotalIngresos { get; set; }
    public int TotalEgresos { get; set; }
    public int Balance => TotalIngresos - TotalEgresos;
}
