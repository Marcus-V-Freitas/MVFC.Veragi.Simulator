using MVFC.Veragi.Simulator.Shareable.Responses.Reconciliation;
using MVFC.Veragi.Simulator.Shareable.Responses.Webhooks;
using MVFC.Veragi.Simulator.Domain.Mappings;
using MVFC.Veragi.Simulator.Domain.Ports;

namespace MVFC.Veragi.Simulator.Domain.Services.Simulation;

public sealed class InspectionService(ISimulatorStore store)
{
    private readonly ISimulatorStore _store = store;

    public async Task<IReadOnlyList<WebhookDeliveryResponse>> ListDeliveriesAsync(
        string? kind,
        string? merchantCnpj,
        CancellationToken cancellationToken
    ) => [.. (await _store.GetDeliveriesAsync(cancellationToken)).Where(delivery => (kind is null || delivery.Kind == kind) && (merchantCnpj is null || delivery.MerchantCnpj == merchantCnpj)).Select(delivery => delivery.ToResponse())];

    public async Task<IReadOnlyList<WebhookReceiptResponse>> ListReceiptsAsync(
        string? kind,
        string? merchantCnpj,
        string? requestId,
        CancellationToken cancellationToken
    ) => [.. (await _store.GetReceiptsAsync(cancellationToken)).Where(receipt => (kind is null || receipt.Kind == kind) && (merchantCnpj is null || receipt.MerchantCnpj == merchantCnpj) && (requestId is null || receipt.RequestId == requestId)).Select(receipt => receipt.ToResponse())];

    public async Task<IReadOnlyList<ReconciliationEntryResponse>> ListEntriesAsync(
        string? merchantCnpj,
        CancellationToken cancellationToken
    ) => [.. (await _store.GetEntriesAsync(merchantCnpj, cancellationToken)).Select(entry => entry.ToResponse())];
}
