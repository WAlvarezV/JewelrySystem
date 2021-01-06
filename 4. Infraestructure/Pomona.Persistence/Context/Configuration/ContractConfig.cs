using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Pomona.Domain.Entity;
using Pomona.Domain.Enum;

namespace Pomona.Persistence.Context.Configuration
{
    internal class ContractConfig : IEntityTypeConfiguration<Contract>
    {
        public void Configure(EntityTypeBuilder<Contract> builder)
        {
            builder.HasIndex(p => p.Number)
                .IsUnique();

            var converter = new EnumToStringConverter<ContractState>();
            builder.Property(p => p.State).HasMaxLength(20).HasConversion(converter);

            // builder.HasData(Json<Contract>.GetSeed());
        }
    }
}
