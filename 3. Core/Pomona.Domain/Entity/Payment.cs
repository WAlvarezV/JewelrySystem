using Pomona.Domain.Enum;
using System;

namespace Pomona.Domain.Entity
{
    internal class Payment : BaseEntity
    {
        public string Number { get; set; }
        public DateTime Date { get; set; }
        public int Value { get; set; }
        public int EntityId { get; set; }
        public PaymentType PaymentType { get; set; }
    }
}
