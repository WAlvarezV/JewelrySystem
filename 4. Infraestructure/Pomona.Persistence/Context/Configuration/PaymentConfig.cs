using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Pomona.Domain.Entity;
using Pomona.Domain.Enum;

namespace Pomona.Persistence.Context.Configuration
{
    internal class PaymentConfig : IEntityTypeConfiguration<Payment>
    {
        public void Configure(EntityTypeBuilder<Payment> builder)
        {
            var typeConverter = new EnumToStringConverter<PaymentType>();
            builder.Property(p => p.PaymentType).HasMaxLength(20).HasConversion(typeConverter);

            // builder.HasData(Json<Payment>.GetSeed());
        }
    }
}
