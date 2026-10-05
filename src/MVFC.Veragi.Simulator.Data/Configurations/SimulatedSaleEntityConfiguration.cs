using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MongoDB.EntityFrameworkCore.Extensions;
using MVFC.Veragi.Simulator.Domain.Entities;

namespace MVFC.Veragi.Simulator.Data.Configurations;

public sealed class SimulatedSaleEntityConfiguration : IEntityTypeConfiguration<SimulatedSaleEntity>
{
    public void Configure(EntityTypeBuilder<SimulatedSaleEntity> builder)
    {
        builder.ToCollection("simulated_sales");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
    }
}
