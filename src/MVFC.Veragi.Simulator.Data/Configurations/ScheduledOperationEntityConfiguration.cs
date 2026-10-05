using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MongoDB.EntityFrameworkCore.Extensions;
using MVFC.Veragi.Simulator.Domain.Entities;

namespace MVFC.Veragi.Simulator.Data.Configurations;

public sealed class ScheduledOperationEntityConfiguration : IEntityTypeConfiguration<ScheduledOperationEntity>
{
    public void Configure(EntityTypeBuilder<ScheduledOperationEntity> builder)
    {
        builder.ToCollection("operations");
        builder.Property(x => x.Status).HasConversion<string>();
        builder.Property(x => x.Outcome).HasConversion<string>();
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
    }
}
