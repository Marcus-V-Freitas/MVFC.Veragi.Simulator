using Microsoft.EntityFrameworkCore;
using MVFC.Veragi.Simulator.Domain.Entities;

namespace MVFC.Veragi.Simulator.Data;

public sealed class SimulatorDbContext(DbContextOptions<SimulatorDbContext> options) : DbContext(options)
{
    public DbSet<MerchantEntity> Merchants => Set<MerchantEntity>();

    public DbSet<ScheduledOperationEntity> Operations => Set<ScheduledOperationEntity>();

    public DbSet<ReconciliationEntity> Entries => Set<ReconciliationEntity>();

    public DbSet<WebhookReceiptEntity> Receipts => Set<WebhookReceiptEntity>();

    public DbSet<WebhookDeliveryEntity> Deliveries => Set<WebhookDeliveryEntity>();

    public DbSet<SimulatedSaleEntity> Sales => Set<SimulatedSaleEntity>();

    public DbSet<SalesBatchEntity> SalesBatches => Set<SalesBatchEntity>();

    public DbSet<SimulationScenarioEntity> Scenarios => Set<SimulationScenarioEntity>();

    public DbSet<ExternalAnticipationEntity> ExternalAnticipations => Set<ExternalAnticipationEntity>();

    public DbSet<WebhookRoutingEntity> WebhookRouting => Set<WebhookRoutingEntity>();

    public DbSet<ContractAvailabilityConfigurationEntity> ContractAvailability => Set<ContractAvailabilityConfigurationEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SimulatorDbContext).Assembly);
}
