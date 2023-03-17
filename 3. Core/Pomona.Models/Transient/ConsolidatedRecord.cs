using System;

namespace Pomona.Models.Transient
{
    public class ConsolidatedRecord
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public int Value { get; set; }
        public string RecordType { get; set; }
        public string PaymentMethod { get; set; }
    }
}
