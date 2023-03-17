using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pomona.Models.Transient;

namespace Pomona.Persistence.Context.Configuration
{
    internal class ConsolidatedRecordConfig : IEntityTypeConfiguration<ConsolidatedRecord>
    {
        public void Configure(EntityTypeBuilder<ConsolidatedRecord> builder) => builder.HasNoKey();
    }
}
