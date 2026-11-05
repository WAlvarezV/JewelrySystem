using JewerlySystem.Modules.Inventory.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace JewerlySystem.Modules.Inventory.Infrastructure;

public class BrandConfig : IEntityTypeConfiguration<Brand>
{
    public void Configure(EntityTypeBuilder<Brand> builder)
    {
        builder.ToTable("Brands");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Name).HasMaxLength(300);
    }
}

public class ItemTypeConfig : IEntityTypeConfiguration<ItemType>
{
    public void Configure(EntityTypeBuilder<ItemType> builder)
    {
        builder.ToTable("ItemTypes");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Name).IsRequired().HasMaxLength(25);
    }
}

public class ItemConfig : IEntityTypeConfiguration<Item>
{
    public void Configure(EntityTypeBuilder<Item> builder)
    {
        builder.ToTable("Items");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Description).HasMaxLength(2000);
        builder.Property(p => p.ImageUrl).HasMaxLength(1000).IsRequired(false);
        builder.Property(p => p.ThumbnailUrl).HasMaxLength(1000).IsRequired(false);
    }
}

public class JewelConfig : IEntityTypeConfiguration<Jewel>
{
    public void Configure(EntityTypeBuilder<Jewel> builder)
    {
        builder.ToTable("Jewelry");
        builder.HasKey(p => p.Id);
    }
}

public class WatchConfig : IEntityTypeConfiguration<Watch>
{
    public void Configure(EntityTypeBuilder<Watch> builder)
    {
        builder.ToTable("Watches");
        builder.HasKey(p => p.Id);

        var converter = new EnumToStringConverter<GenderType>();
        builder.Property(p => p.GenderType)
            .HasMaxLength(30)
            .HasConversion(converter);
        builder.Property(p => p.Model).HasMaxLength(150).IsRequired(false);
    }
}
