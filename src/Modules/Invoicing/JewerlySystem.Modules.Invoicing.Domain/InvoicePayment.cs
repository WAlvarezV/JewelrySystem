using JewerlySystem.Common.Domain;

namespace JewerlySystem.Modules.Invoicing.Domain;

public class InvoicePayment : AuditableEntity<int>
{
    public string Number { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public int Value { get; set; }
    public int EntityId { get; set; } // Referencia al ID de la Factura
    public string PaymentType { get; set; } = "Factura";
    public string PaymentMethod { get; set; } = "EFECTIVO";

    public InvoicePayment() { }

    public InvoicePayment(string number, DateTime date, int value, int entityId, string paymentMethod)
    {
        Number = number;
        Date = date;
        Value = value;
        EntityId = entityId;
        PaymentType = "Factura";
        PaymentMethod = paymentMethod;
    }
}
