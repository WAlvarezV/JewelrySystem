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

        public string GetImagePath()
        {
            var prefix = ItemTypeId switch
            {
                1 => @"D:\Inventario\Anillos\ANL",
                2 => @"D:\Inventario\Aretes\ART",
                3 => @"D:\Inventario\Cadenas\CDN",
                4 => @"D:\Inventario\Pulseras\PLR",
                5 => @"D:\Inventario\Relojes\RLJ",
                _ => @"D:\Inventario\Dijes\DJS"
            };
            string path = $"{prefix}{Reference.ToString().PadLeft(4, '0')}.jpeg";
            return path;
        }
    }
}
