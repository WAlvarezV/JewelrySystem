using System;

namespace Pomona.Models.Transient
{
    public class ConsolidatedRecord
    {
        public Int64 Id { get; set; }
        public string Date { get; set; }
        public int Value { get; set; }
        public string RecordType { get; set; }
        public string PaymentMethod { get; set; }
    }
}
