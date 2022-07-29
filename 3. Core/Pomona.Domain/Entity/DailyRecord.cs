
using Pomona.Domain.Enum;
using System;

namespace Pomona.Domain.Entity
{
    internal class DailyRecord : BaseEntity
    {
        public DateTime Date { get; set; }
        public string Description { get; set; }
        public int Value { get; set; }
        public RecordType RecordType { get; set; }
        public PaymentMethod PaymentMethod { get; set; }

        public int? ItemId { get; set; }
        public Item Item { get; set; }
    }
}
