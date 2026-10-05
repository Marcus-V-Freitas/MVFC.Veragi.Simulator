using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MongoDB.EntityFrameworkCore.Extensions;
using MVFC.Veragi.Simulator.Domain.Entities;

namespace MVFC.Veragi.Simulator.Data.Configurations;

public sealed class ReconciliationEntityConfiguration : IEntityTypeConfiguration<ReconciliationEntity>
{
    public void Configure(EntityTypeBuilder<ReconciliationEntity> builder)
    {
        builder.ToCollection("reconciliation_entries");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
    }
}
