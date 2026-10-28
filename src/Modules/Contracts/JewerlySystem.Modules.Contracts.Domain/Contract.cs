using JewerlySystem.Common.Domain;

namespace JewerlySystem.Modules.Contracts.Domain;

public class Contract : AuditableEntity<int>
{
    public int Number { get; set; }
    public int? Reference { get; set; }
    public DateTime Date { get; set; }
    public DateTime DeliveryDate { get; set; }
    public int Value { get; set; }
    public ContractState State { get; set; }
    public int Balance { get; set; }
    public double Weight { get; set; }
    public string Description { get; set; } = string.Empty;

    public int PersonId { get; set; }
    public int? ProviderId { get; set; }
    public int? FatherId { get; set; }

    public Contract() { }

    public Contract(
        int number,
        int? reference,
        DateTime date,
        DateTime deliveryDate,
        int value,
        ContractState state,
        int balance,
        double weight,
        string description,
        int personId,
        int? providerId = null,
        int? fatherId = null)
    {
        Number = number;
        Reference = reference;
        Date = date;
        DeliveryDate = deliveryDate;
        Value = value;
        State = state;
        Balance = balance;
        Weight = weight;
        Description = description ?? string.Empty;
        PersonId = personId;
        ProviderId = providerId;
        FatherId = fatherId;
    }

    public void RegisterPayment(int paymentValue)
    {
        Balance = Math.Max(0, Balance - paymentValue);
        if (Balance == 0)
        {
            State = ContractState.Recibido;
        }
    }
}
