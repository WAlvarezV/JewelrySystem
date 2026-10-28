using JewerlySystem.Common.Domain;

namespace JewerlySystem.Modules.Invoicing.Domain;

public class Invoice : AuditableEntity<int>
{
    public string Number { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public DateTime PaymentLimitDate { get; set; }
    public int Value { get; set; }
    public int CreditValue { get; set; }
    public int BalanceValue { get; set; }
    public int ProviderId { get; set; }

    public Invoice() { }

    public Invoice(
        string number,
        DateTime date,
        DateTime paymentLimitDate,
        int value,
        int creditValue,
        int balanceValue,
        int providerId)
    {
        Number = number ?? string.Empty;
        Date = date;
        PaymentLimitDate = paymentLimitDate;
        Value = value;
        CreditValue = creditValue;
        BalanceValue = balanceValue;
        ProviderId = providerId;
    }
}
