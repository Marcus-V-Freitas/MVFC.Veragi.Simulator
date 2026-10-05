using System.Text.Json.Nodes;

namespace MVFC.Veragi.Simulator.Shareable.Responses.Reconciliation;

public sealed record ReconciliationEntryResponse(
    Guid EntryId,
    string MerchantCnpj,
    JsonNode? Entry,
    JsonNode? Receivables,
    decimal UnallocatedAmount
);
