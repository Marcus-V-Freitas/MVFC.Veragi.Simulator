using MVFC.Veragi.Simulator.Domain.Entities;

namespace MVFC.Veragi.Simulator.Domain.Ports;

public interface ISimulatorStore
{
    Task<MerchantEntity?> GetMerchantAsync(string cnpj, CancellationToken ct);
    Task<ScheduledOperationEntity?> GetOperationAsync(Guid id, CancellationToken ct);
    Task<List<ScheduledOperationEntity>> GetOperationsAsync(string kind, string? merchantCnpj, CancellationToken ct);
    Task<List<ScheduledOperationEntity>> GetDueOperationsAsync(string kind, DateTime now, CancellationToken ct);
    Task<List<WebhookDeliveryEntity>> GetPendingDeliveriesAsync(DateTime now, CancellationToken ct);
    Task<ReconciliationEntity?> GetEntryAsync(Guid id, CancellationToken ct);
    Task<WebhookReceiptEntity?> GetReceiptAsync(string id, CancellationToken ct);
    Task<List<WebhookReceiptEntity>> GetReceiptsAsync(CancellationToken ct);
    Task<WebhookDeliveryEntity?> GetDeliveryAsync(string id, CancellationToken ct);
    Task<WebhookDeliveryEntity?> GetDeliveryByPayloadKeyAsync(string key, CancellationToken ct);
    Task<List<WebhookDeliveryEntity>> GetDeliveriesAsync(CancellationToken ct);
    Task<List<SimulatedSaleEntity>> GetSalesAsync(string cnpj, CancellationToken ct);
    Task<SalesBatchEntity?> GetSalesBatchAsync(string id, CancellationToken ct);
    void AddSale(SimulatedSaleEntity sale);
    void AddSalesBatch(SalesBatchEntity batch);
    void AddDelivery(WebhookDeliveryEntity delivery);
    void AddMerchant(MerchantEntity merchant);
    void AddOperation(ScheduledOperationEntity operation);
    void AddEntry(ReconciliationEntity entry);
    void AddReceipt(WebhookReceiptEntity receipt);
    Task<SimulationScenarioEntity?> GetScenarioAsync(string id, CancellationToken ct);
    Task<List<SimulationScenarioEntity>> GetScenariosAsync(CancellationToken ct);
    void AddScenario(SimulationScenarioEntity scenario);
    void RemoveScenario(SimulationScenarioEntity scenario);
    Task<List<ExternalAnticipationEntity>> GetExternalAnticipationsAsync(string cnpj, CancellationToken ct);
    void AddExternalAnticipation(ExternalAnticipationEntity anticipation);
    Task<List<ReconciliationEntity>> GetEntriesAsync(string? cnpj, CancellationToken ct);
    Task<int> ResetAsync(CancellationToken ct);
    Task<WebhookRoutingEntity?> GetWebhookRoutingAsync(CancellationToken ct);
    void AddWebhookRouting(WebhookRoutingEntity routing);
    Task<ContractAvailabilityConfigurationEntity?> GetContractAvailabilityAsync(CancellationToken ct);
    void AddContractAvailability(ContractAvailabilityConfigurationEntity configuration);
    Task SaveAsync(CancellationToken ct);
}
