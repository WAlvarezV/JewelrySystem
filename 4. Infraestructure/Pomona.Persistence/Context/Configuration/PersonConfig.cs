using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Pomona.Domain.Entity;
using Pomona.Domain.Enum;

namespace Pomona.Persistence.Context.Configuration
{
    internal class PersonConfig : IEntityTypeConfiguration<Person>
    {
        public void Configure(EntityTypeBuilder<Person> builder)
        {
            builder.HasIndex(p => p.IdentificationNumber)
                .IsUnique();

            builder.Property(p => p.FullName).HasMaxLength(500);
            builder.Property(p => p.CellPhone).HasMaxLength(15);
            builder.Property(p => p.IdentificationNumber).HasMaxLength(15);
            builder.Property(p => p.Email).HasMaxLength(150);

            var typeConverter = new EnumToStringConverter<PersonType>();
            builder.Property(p => p.PersonType).HasMaxLength(20).HasConversion(typeConverter);

            // builder.HasData(Json<Person>.GetSeed());
        }
    }
}
