using Pomona.Domain.Enum;
using System;

namespace Pomona.Domain.Entity
{
    internal class Contract : BaseEntity
    {
        public int Number { get; set; }
        public DateTime Date { get; set; }
        public DateTime DeliveryDate { get; set; }
        public int Value { get; set; }
        public ContractState State { get; set; }
        public int Balance { get; set; }
        public double Weight { get; set; }
        public string Description { get; set; }
    }
}
