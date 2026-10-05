using MVFC.Veragi.Simulator.Shareable.Requests.Reconciliation;
using MVFC.Veragi.Simulator.Shareable.Responses.Merchants;
using MVFC.Veragi.Simulator.Domain.Behaviors;
using MediatR;
using OperationResult;

namespace MVFC.Veragi.Simulator.Domain.Handlers.Reconciliation.CreateReconciliation;

public sealed record CreateReconciliationCommand(BankReconciliationEntry Request) : SimulatedCommand<bool>
{
    public override string Target => "reconciliation";
    public override string? MerchantCnpj => Request.Merchant;
}
