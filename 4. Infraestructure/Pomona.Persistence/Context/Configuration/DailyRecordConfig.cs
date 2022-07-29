using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Pomona.Domain.Entity;
using Pomona.Domain.Enum;

namespace Pomona.Persistence.Context.Configuration
{
    internal class DailyRecordConfig : IEntityTypeConfiguration<DailyRecord>
    {
        public void Configure(EntityTypeBuilder<DailyRecord> builder)
        {
            builder.Property(p => p.Description).HasMaxLength(2000);

            var recordTypeConverter = new EnumToStringConverter<RecordType>();
            builder.Property(p => p.RecordType).HasMaxLength(20).HasConversion(recordTypeConverter);

            var paymentMethodConverter = new EnumToStringConverter<PaymentMethod>();
            builder.Property(p => p.PaymentMethod).HasMaxLength(20).HasConversion(paymentMethodConverter);

            // builder.HasData(Json<DailyRecord>.GetSeed());
        }
    }
}
