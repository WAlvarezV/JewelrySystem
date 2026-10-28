using JewerlySystem.Modules.Cash.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace JewerlySystem.Modules.Cash.Infrastructure;

public class DailyRecordConfig : IEntityTypeConfiguration<DailyRecord>
{
    public void Configure(EntityTypeBuilder<DailyRecord> builder)
    {
        builder.ToTable("DailyRecords");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Number).HasMaxLength(10);
        builder.Property(p => p.Description).HasMaxLength(2000);

        var recordTypeConverter = new EnumToStringConverter<RecordType>();
        builder.Property(p => p.RecordType)
            .HasMaxLength(20)
            .HasConversion(recordTypeConverter);

        var paymentMethodConverter = new EnumToStringConverter<PaymentMethod>();
        builder.Property(p => p.PaymentMethod)
            .HasMaxLength(20)
            .HasConversion(paymentMethodConverter);
    }
}

public class ConsolidatedRecordConfig : IEntityTypeConfiguration<ConsolidatedRecord>
{
    public void Configure(EntityTypeBuilder<ConsolidatedRecord> builder)
    {
        builder.ToTable("ConsolidatedRecord");
        builder.HasNoKey();
    }
}
