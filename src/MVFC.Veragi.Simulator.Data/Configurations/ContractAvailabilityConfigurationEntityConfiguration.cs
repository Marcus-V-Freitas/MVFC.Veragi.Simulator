using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MongoDB.EntityFrameworkCore.Extensions;
using MVFC.Veragi.Simulator.Domain.Entities;

namespace MVFC.Veragi.Simulator.Data.Configurations;

public sealed class ContractAvailabilityConfigurationEntityConfiguration : IEntityTypeConfiguration<ContractAvailabilityConfigurationEntity>
{
    public void Configure(EntityTypeBuilder<ContractAvailabilityConfigurationEntity> builder)
    {
        builder.ToCollection("contract_availability_configuration");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).ValueGeneratedNever();
    }
}
