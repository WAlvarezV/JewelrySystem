using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pomona.Domain.Entity;
using Pomona.Utilities.Functions;

namespace Pomona.Persistence.Context.Configuration
{
    internal class BrandConfig : IEntityTypeConfiguration<Brand>
    {
        public void Configure(EntityTypeBuilder<Brand> builder)
        {
            builder.Property(p => p.Name).HasMaxLength(300);
            builder.HasData(Json<Brand>.GetSeed());
        }
    }
}
