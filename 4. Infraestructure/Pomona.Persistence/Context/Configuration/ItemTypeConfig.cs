using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pomona.Domain.Entity;
using Pomona.Utilities.Functions;

namespace Pomona.Persistence.Context.Configuration
{
    internal class ItemTypeConfig : IEntityTypeConfiguration<ItemType>
    {
        public void Configure(EntityTypeBuilder<ItemType> builder)
        {
            //var name = nameof(ItemType);

            builder.Property(p => p.Name)
                .IsRequired()
                .HasMaxLength(25);

            builder.HasData(Json<ItemType>.GetSeed());
        }
    }
}
