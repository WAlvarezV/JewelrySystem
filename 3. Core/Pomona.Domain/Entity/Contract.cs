using Pomona.Domain.Enum;
using System;

namespace Pomona.Domain.Entity
{
    internal class Contract : BaseEntity
    {
        public int Number { get; set; }
        public int? Reference { get; set; }
        public DateTime Date { get; set; }
        public DateTime DeliveryDate { get; set; }
        public int Value { get; set; }
        public ContractState State { get; set; }
        public int Balance { get; set; }
        public double Weight { get; set; }
        public string Description { get; set; }

        public int PersonId { get; set; }
        public Person Person { get; set; }
        public int? ProviderId { get; set; }
        public virtual Person Provider { get; set; }
        public int? FatherId { get; set; }
        public virtual Contract Father { get; set; }
    }
}
