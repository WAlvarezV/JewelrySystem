using JewerlySystem.Modules.Customers.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace JewerlySystem.Modules.Customers.Infrastructure;

public class IdentificationTypeConfig : IEntityTypeConfiguration<IdentificationType>
{
    public void Configure(EntityTypeBuilder<IdentificationType> builder)
    {
        builder.ToTable("IdentificationTypes");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(50);
    }
}

public class PersonConfig : IEntityTypeConfiguration<Person>
{
    public void Configure(EntityTypeBuilder<Person> builder)
    {
        builder.ToTable("Persons");
        builder.HasKey(p => p.Id);

        builder.HasIndex(p => p.IdentificationNumber)
            .IsUnique();

        builder.HasIndex(p => p.UserId);

        builder.Property(p => p.UserId).HasMaxLength(450);
        builder.Property(p => p.FirstName).HasMaxLength(100);
        builder.Property(p => p.FirstLastName).HasMaxLength(100);
        builder.Property(p => p.SecondLastName).HasMaxLength(100);
        builder.Property(p => p.FullName).HasMaxLength(500);
        builder.Property(p => p.Address).HasMaxLength(200);
        builder.Property(p => p.CellPhone).HasMaxLength(50);
        builder.Property(p => p.IdentificationNumber).HasMaxLength(50);
        builder.Property(p => p.Email).HasMaxLength(150);

        var typeConverter = new EnumToStringConverter<PersonType>();
        builder.Property(p => p.PersonType)
            .HasMaxLength(20)
            .HasConversion(typeConverter);
    }
}
