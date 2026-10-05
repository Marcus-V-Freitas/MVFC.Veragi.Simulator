using MVFC.Veragi.Simulator.Domain.Services.Reconciliation;
using MediatR;
using OperationResult;

namespace MVFC.Veragi.Simulator.Domain.Handlers.Reconciliation.CreateReconciliation;

public sealed class CreateReconciliationHandler(ReconciliationService reconciliation) : IRequestHandler<CreateReconciliationCommand, Result<bool>>
{
    private readonly ReconciliationService _reconciliation = reconciliation;

    public Task<Result<bool>> Handle(CreateReconciliationCommand command, CancellationToken cancellationToken) =>
        _reconciliation.CreateAsync(command.Request, cancellationToken);
}
