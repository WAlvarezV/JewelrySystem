using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Pomona.Domain.Entity;
using Pomona.Domain.Enum;

namespace Pomona.Persistence.Context.Configuration
{
    internal class WatchConfig : IEntityTypeConfiguration<Watch>
    {
        public void Configure(EntityTypeBuilder<Watch> builder)
        {
            var converter = new EnumToStringConverter<GenderType>();
            builder.Property(p => p.GenderType).HasMaxLength(30).HasConversion(converter);

            // builder.HasData(Json<Watch>.GetSeed());
        }
    }
}
