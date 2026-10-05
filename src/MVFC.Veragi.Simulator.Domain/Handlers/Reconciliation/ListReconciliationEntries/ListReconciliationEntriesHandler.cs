using MVFC.Veragi.Simulator.Domain.Services.Simulation;
using MVFC.Veragi.Simulator.Shareable.Responses.Reconciliation;
using MediatR;

namespace MVFC.Veragi.Simulator.Domain.Handlers.Reconciliation.ListReconciliationEntries;

public sealed class ListReconciliationEntriesHandler(InspectionService inspection) : IRequestHandler<ListReconciliationEntriesCommand, IReadOnlyList<ReconciliationEntryResponse>>
{
    private readonly InspectionService _inspection = inspection;

    public Task<IReadOnlyList<ReconciliationEntryResponse>> Handle(ListReconciliationEntriesCommand command, CancellationToken cancellationToken) =>
        _inspection.ListEntriesAsync(command.MerchantCnpj, cancellationToken);
}
