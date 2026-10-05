using MVFC.Veragi.Simulator.Domain.Ports;
using MVFC.Veragi.Simulator.Shareable.Enums;
using MVFC.Veragi.Simulator.Shareable.Extensions;
using MVFC.Veragi.Simulator.Shareable.Responses.Contracts;
using MVFC.Veragi.Simulator.Shareable.Responses.Reconciliation;

namespace MVFC.Veragi.Simulator.Domain.Services.Contracts;

public sealed class ContractDebtService(ISimulatorStore store)
{
    private readonly ISimulatorStore _store = store;

    public async Task<ContractByExternalReference> ProjectAsync(
        string merchantCnpj,
        string externalReference,
        ContractByExternalReference registered,
        CancellationToken cancellationToken)
    {
        if (registered.Status != ContractStatusType.Active || registered.DebtBalanceAmount is null)
            return registered;

        var entries = await _store.GetEntriesAsync(merchantCnpj, cancellationToken);
        var paid = entries.SelectMany(entry => entry.AllocationsJson.FromJson<List<ReconciledReceivableResponse>>()!)
            .Where(unit => unit.ExternalReference == externalReference)
            .Sum(unit => unit.Amount);

        return registered with { DebtBalanceAmount = Math.Max(0, registered.DebtBalanceAmount.Value - paid) };
    }
}
