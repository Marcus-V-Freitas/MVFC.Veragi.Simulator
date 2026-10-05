using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MongoDB.EntityFrameworkCore.Extensions;
using MVFC.Veragi.Simulator.Domain.Entities;

namespace MVFC.Veragi.Simulator.Data.Configurations;

public sealed class SalesBatchEntityConfiguration : IEntityTypeConfiguration<SalesBatchEntity>
{
    public void Configure(EntityTypeBuilder<SalesBatchEntity> builder)
    {
        builder.ToCollection("sales_batches");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
    }
}
