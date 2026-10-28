using JewerlySystem.Modules.Contracts.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace JewerlySystem.Modules.Contracts.Infrastructure;

public class ContractConfig : IEntityTypeConfiguration<Contract>
{
    public void Configure(EntityTypeBuilder<Contract> builder)
    {
        builder.ToTable("Contracts");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Number).HasMaxLength(5);
        builder.Property(p => p.Description).HasMaxLength(2000);

        builder.HasIndex(p => p.Number)
            .IsUnique();

        var converter = new EnumToStringConverter<ContractState>();
        builder.Property(p => p.State)
            .HasMaxLength(20)
            .HasConversion(converter);
    }
}

public class PaymentConfig : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("Payments");
        builder.HasKey(p => p.Id);

        var typeConverter = new EnumToStringConverter<PaymentType>();
        builder.Property(p => p.PaymentType)
            .HasMaxLength(20)
            .HasConversion(typeConverter);

        var methodConverter = new EnumToStringConverter<PaymentMethod>();
        builder.Property(p => p.PaymentMethod)
            .HasMaxLength(20)
            .HasConversion(methodConverter);
    }
}
