using Pomona.Domain.Enum;

namespace Pomona.Domain.Entity
{
    internal class Watch : BaseEntity
    {
        public int ItemId { get; set; }
        public Item Item { get; set; }

        public GenderType GenderType { get; set; }
        public string CaseNumber { get; set; }

        public int BrandId { get; set; }
        public Brand Brand { get; set; }
    }
}
