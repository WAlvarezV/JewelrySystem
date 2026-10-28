using JewerlySystem.Common.Domain;

namespace JewerlySystem.Modules.Contracts.Domain;

public class Payment : AuditableEntity<int>
{
    public string Number { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public int Value { get; set; }
    public int EntityId { get; set; }
    public PaymentType PaymentType { get; set; }
    public PaymentMethod PaymentMethod { get; set; }

    public Payment() { }

    public Payment(
        string number,
        DateTime date,
        int value,
        int entityId,
        PaymentType paymentType,
        PaymentMethod paymentMethod)
    {
        Number = number ?? string.Empty;
        Date = date;
        Value = value;
        EntityId = entityId;
        PaymentType = paymentType;
        PaymentMethod = paymentMethod;
    }
}
