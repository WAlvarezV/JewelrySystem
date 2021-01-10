
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pomona.Domain.Entity;
using Pomona.Utilities.Functions;

namespace Pomona.Persistence.Context.Configuration
{
    internal class IdentificationTypeConfig : IEntityTypeConfiguration<IdentificationType>
    {
        public void Configure(EntityTypeBuilder<IdentificationType> builder)
        {
            //var name = nameof(IdentificationType);

            builder.Property(p => p.Name)
                .IsRequired()
                .HasMaxLength(50);

            builder.HasData(Json<IdentificationType>.GetSeed());
        }
    }
}
