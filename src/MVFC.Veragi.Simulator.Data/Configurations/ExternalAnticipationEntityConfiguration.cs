using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MongoDB.EntityFrameworkCore.Extensions;
using MVFC.Veragi.Simulator.Domain.Entities;

namespace MVFC.Veragi.Simulator.Data.Configurations;

public sealed class ExternalAnticipationEntityConfiguration : IEntityTypeConfiguration<ExternalAnticipationEntity>
{
    public void Configure(EntityTypeBuilder<ExternalAnticipationEntity> builder)
    {
        builder.ToCollection("external_anticipations");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
    }
}
