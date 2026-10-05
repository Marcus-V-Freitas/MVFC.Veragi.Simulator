using Microsoft.EntityFrameworkCore;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.EntityFrameworkCore.Extensions;
using MVFC.Veragi.Simulator.Domain.Entities;
using MVFC.Veragi.Simulator.Domain.Ports;
using MVFC.Veragi.Simulator.Shareable.Enums;

namespace MVFC.Veragi.Simulator.Data;

public sealed class MongoSimulatorStore(SimulatorDbContext context, IMongoDatabase database) : ISimulatorStore
{
    private readonly SimulatorDbContext _context = context;
    private readonly IMongoDatabase _database = database;

    public Task<MerchantEntity?> GetMerchantAsync(string cnpj, CancellationToken ct) =>
        _context.Merchants.SingleOrDefaultAsync(x => x.Cnpj == cnpj, ct);

    public Task<ScheduledOperationEntity?> GetOperationAsync(Guid id, CancellationToken ct) =>
        _context.Operations.SingleOrDefaultAsync(x => x.Id == id, ct);

    public Task<List<ScheduledOperationEntity>> GetOperationsAsync(string kind, string? merchantCnpj, CancellationToken ct) =>
        _context.Operations
            .Where(x => x.Kind == kind && (merchantCnpj == null || x.MerchantCnpj == merchantCnpj))
            .ToListAsync(ct);

    public Task<List<ScheduledOperationEntity>> GetDueOperationsAsync(string kind, DateTime now, CancellationToken ct) =>
        _context.Operations
            .Where(x => x.Kind == kind && x.Status == ScheduleQueryStatusType.PROCESSING && x.DueAt <= now)
            .Take(100)
            .ToListAsync(ct);

    public Task<List<WebhookDeliveryEntity>> GetPendingDeliveriesAsync(DateTime now, CancellationToken ct) =>
        _context.Deliveries
            .Where(x => !x.Delivered && !x.DeadLetter && x.NextAttemptAt <= now)
            .Take(100)
            .ToListAsync(ct);

    public Task<WebhookDeliveryEntity?> GetDeliveryAsync(string id, CancellationToken ct) =>
        Guid.TryParse(id, out var guid)
            ? _context.Deliveries.SingleOrDefaultAsync(x => x.Id == guid, ct)
            : Task.FromResult<WebhookDeliveryEntity?>(null);

    public Task<WebhookDeliveryEntity?> GetDeliveryByPayloadKeyAsync(string key, CancellationToken ct) =>
        _context.Deliveries.SingleOrDefaultAsync(x => x.PayloadKey == key, ct);

    public Task<List<WebhookDeliveryEntity>> GetDeliveriesAsync(CancellationToken ct) =>
        _context.Deliveries.ToListAsync(ct);

    public Task<ReconciliationEntity?> GetEntryAsync(Guid id, CancellationToken ct) =>
        _context.Entries.SingleOrDefaultAsync(x => x.EntryId == id, ct);

    public Task<WebhookReceiptEntity?> GetReceiptAsync(string id, CancellationToken ct) =>
        _context.Receipts.SingleOrDefaultAsync(x => x.EventKey == id, ct);

    public Task<List<WebhookReceiptEntity>> GetReceiptsAsync(CancellationToken ct) =>
        _context.Receipts.ToListAsync(ct);

    public void AddMerchant(MerchantEntity merchant) =>
        _context.Merchants.Add(merchant);

    public void AddOperation(ScheduledOperationEntity operation) =>
        _context.Operations.Add(operation);

    public void AddEntry(ReconciliationEntity entry) =>
        _context.Entries.Add(entry);

    public void AddReceipt(WebhookReceiptEntity receipt) =>
        _context.Receipts.Add(receipt);

    public void AddDelivery(WebhookDeliveryEntity delivery) =>
        _context.Deliveries.Add(delivery);

    public Task<List<SimulatedSaleEntity>> GetSalesAsync(string cnpj, CancellationToken ct) =>
        _context.Sales
            .Where(x => x.MerchantCnpj == cnpj)
            .ToListAsync(ct);

    public Task<SalesBatchEntity?> GetSalesBatchAsync(string id, CancellationToken ct) =>
        _context.SalesBatches.SingleOrDefaultAsync(x => x.BatchKey == id, ct);

    public void AddSale(SimulatedSaleEntity sale) =>
        _context.Sales.Add(sale);

    public void AddSalesBatch(SalesBatchEntity batch) =>
        _context.SalesBatches.Add(batch);

    public Task<SimulationScenarioEntity?> GetScenarioAsync(string id, CancellationToken ct) =>
        _context.Scenarios.SingleOrDefaultAsync(x => x.ScopeKey == id, ct);

    public Task<List<SimulationScenarioEntity>> GetScenariosAsync(CancellationToken ct) =>
        _context.Scenarios.ToListAsync(ct);

    public void AddScenario(SimulationScenarioEntity scenario) =>
        _context.Scenarios.Add(scenario);

    public void RemoveScenario(SimulationScenarioEntity scenario) =>
        _context.Scenarios.Remove(scenario);

    public Task<List<ExternalAnticipationEntity>> GetExternalAnticipationsAsync(string cnpj, CancellationToken ct) =>
        _context.ExternalAnticipations
            .Where(x => x.MerchantCnpj == cnpj)
            .ToListAsync(ct);

    public void AddExternalAnticipation(ExternalAnticipationEntity anticipation) =>
        _context.ExternalAnticipations.Add(anticipation);

    public Task<List<ReconciliationEntity>> GetEntriesAsync(string? cnpj, CancellationToken ct) =>
        _context.Entries
            .Where(x => cnpj == null || x.MerchantCnpj == cnpj)
            .ToListAsync(ct);

    public async Task<int> ResetAsync(CancellationToken ct)
    {
        var collections = _context.Model.GetCollectionNames().ToArray();
        using var session = await _database.Client.StartSessionAsync(cancellationToken: ct);

        var removed = await session.WithTransactionAsync(async (transaction, token) =>
        {
            var count = 0;

            foreach (var name in collections)
            {
                var result = await _database
                    .GetCollection<BsonDocument>(name)
                    .DeleteManyAsync(
                        transaction,
                        FilterDefinition<BsonDocument>.Empty,
                        cancellationToken: token);

                count += (int)result.DeletedCount;
            }

            return count;
        }, cancellationToken: ct);

        _context.ChangeTracker.Clear();

        return removed;
    }

    public Task<WebhookRoutingEntity?> GetWebhookRoutingAsync(CancellationToken ct) =>
        _context.WebhookRouting.SingleOrDefaultAsync(ct);

    public void AddWebhookRouting(WebhookRoutingEntity routing) =>
        _context.WebhookRouting.Add(routing);

    public Task<ContractAvailabilityConfigurationEntity?> GetContractAvailabilityAsync(CancellationToken ct) =>
        _context.ContractAvailability.SingleOrDefaultAsync(ct);

    public void AddContractAvailability(ContractAvailabilityConfigurationEntity configuration) =>
        _context.ContractAvailability.Add(configuration);

    public Task SaveAsync(CancellationToken ct) =>
        _context.SaveChangesAsync(ct);
}
