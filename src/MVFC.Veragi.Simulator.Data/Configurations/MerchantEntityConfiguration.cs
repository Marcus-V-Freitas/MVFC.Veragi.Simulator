using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MongoDB.EntityFrameworkCore.Extensions;
using MVFC.Veragi.Simulator.Domain.Entities;

namespace MVFC.Veragi.Simulator.Data.Configurations;

public sealed class MerchantEntityConfiguration : IEntityTypeConfiguration<MerchantEntity>
{
    public void Configure(EntityTypeBuilder<MerchantEntity> builder)
    {
        builder.ToCollection("merchants");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
    }
}
