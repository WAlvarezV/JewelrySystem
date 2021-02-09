using System;

namespace Pomona.Domain.Entity
{
    internal class Item : BaseEntity
    {
        public int Reference { get; set; }
        public int CostValue { get; set; }
        public DateTime DateOfEntry { get; set; }
        public DateTime? DateOfSale { get; set; }
        public int? SaleValue { get; set; }
        public string Description { get; set; }
        public bool Active { get; set; } = true;

        public int ItemTypeId { get; set; }
        public ItemType ItemType { get; set; }

        public int? ProviderId { get; set; }
        public Person Provider { get; set; }
    }
}
