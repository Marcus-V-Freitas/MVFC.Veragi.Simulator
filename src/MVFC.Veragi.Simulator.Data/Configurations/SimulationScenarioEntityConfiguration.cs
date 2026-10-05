using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MongoDB.EntityFrameworkCore.Extensions;
using MVFC.Veragi.Simulator.Domain.Entities;

namespace MVFC.Veragi.Simulator.Data.Configurations;

public sealed class SimulationScenarioEntityConfiguration : IEntityTypeConfiguration<SimulationScenarioEntity>
{
    public void Configure(EntityTypeBuilder<SimulationScenarioEntity> builder)
    {
        builder.ToCollection("simulation_scenarios");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
    }
}
