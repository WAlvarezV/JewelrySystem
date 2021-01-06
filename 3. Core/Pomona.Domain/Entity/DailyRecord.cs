using System;

namespace Pomona.Domain.Entity
{
    internal class DailyRecord : BaseEntity
    {
        public DateTime Date { get; set; }
        public int Value { get; set; }


    }
}
