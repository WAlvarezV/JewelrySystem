using System;

namespace Pomona.Domain.Entity
{
    internal class Payment : BaseEntity
    {
        public string Number { get; set; }
        public DateTime Date { get; set; }
        public int Value { get; set; }
    }
}
