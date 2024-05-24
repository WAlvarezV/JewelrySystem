
using Pomona.Domain.Enum;
using System;

namespace Pomona.Domain.Entity
{
    internal class DailyRecord : BaseEntity
    {
        public DateTime Date { get; set; }
        public string Number { get; set; }
        public int? Reference { get; set; }
        public string Description { get; set; }
        public int Value { get; set; }
        public RecordType RecordType { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
    }
}
