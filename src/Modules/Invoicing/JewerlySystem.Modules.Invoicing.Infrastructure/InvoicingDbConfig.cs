using JewerlySystem.Modules.Invoicing.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JewerlySystem.Modules.Invoicing.Infrastructure;

public class InvoiceConfig : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.ToTable("Invoices");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Number).HasMaxLength(50);
    }
}
