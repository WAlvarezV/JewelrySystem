using System;
using System.Text.Json;

namespace Pomona.Domain.Entity
{
    internal class Inventory : BaseEntity
    {
        public int Reference { get; set; }
        public int CostValue { get; set; }
        public DateTime DateOfEntry { get; set; }
        public DateTime DateOfSale { get; set; }
        public int SaleValue { get; set; }
        public int ItemTypeId { get; set; }
        public ItemType ItemType { get; set; }
        public JsonDocument Item { get; set; }
        public bool Active { get; set; } = true;

        public string Description { get; set; }
    }
}
