using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pomona.Domain.Entity;

namespace Pomona.Persistence.Context.Configuration
{
    internal class InventoryConfig : IEntityTypeConfiguration<Inventory>
    {
        // JsonDocumentOptions options = new JsonDocumentOptions { AllowTrailingCommas = true };

        public void Configure(EntityTypeBuilder<Inventory> builder)
        {
            builder.Property(p => p.Description).HasMaxLength(2000);

            //builder.Property(p => p.Item)
            //    .IsRequired()
            //     .HasConversion(v => JsonSerializer.Serialize(v, null),
            //     v => JsonDocument.Parse(v, options));

            //builder.Property(p => p.Item)
            //    .IsRequired()
            //     .HasConversion(v => JsonConvert.SerializeObject(v),
            //     v => JsonConvert.DeserializeObject<JObject>(v));

            // builder.HasData(Json<Inventory>.GetSeed());
        }
    }
}
