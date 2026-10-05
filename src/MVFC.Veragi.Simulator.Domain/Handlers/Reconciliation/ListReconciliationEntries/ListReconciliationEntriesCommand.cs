using MVFC.Veragi.Simulator.Shareable.Responses.Reconciliation;
using MediatR;
using OperationResult;

namespace MVFC.Veragi.Simulator.Domain.Handlers.Reconciliation.ListReconciliationEntries;

public sealed record ListReconciliationEntriesCommand(string? MerchantCnpj) : IRequest<IReadOnlyList<ReconciliationEntryResponse>>;
