namespace Pomona.Domain.Entity
{
    internal class Jewel : BaseEntity
    {
        public int ItemId { get; set; }
        public Item Item { get; set; }

        public double Weight { get; set; }
        public int GramValue { get; set; }
        public int? Length { get; set; }
    }
}
