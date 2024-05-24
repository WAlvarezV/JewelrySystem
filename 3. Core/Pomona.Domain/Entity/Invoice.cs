using System;

namespace Pomona.Domain.Entity
{
    internal class Invoice : BaseEntity
    {
        public string Number { get; set; }
        public DateTime Date { get; set; }
        public DateTime PaymentLimitDate { get; set; }
        public int Value { get; set; }
        public int CreditValue { get; set; }
        public int BalanceValue { get; set; }
        public int ProviderId { get; set; }
        public Person Provider { get; set; }

    }
}
