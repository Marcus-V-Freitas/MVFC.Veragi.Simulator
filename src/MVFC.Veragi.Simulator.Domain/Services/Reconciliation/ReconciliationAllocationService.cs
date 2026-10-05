using MVFC.Veragi.Simulator.Domain.Entities;
using MVFC.Veragi.Simulator.Domain.Mappings;
using MVFC.Veragi.Simulator.Domain.Ports;
using MVFC.Veragi.Simulator.Shareable.Extensions;
using MVFC.Veragi.Simulator.Shareable.Responses.Reconciliation;
using System.Linq;

namespace MVFC.Veragi.Simulator.Domain.Services.Reconciliation;

public sealed class ReconciliationAllocationService(ISimulatorStore store)
{
    private readonly ISimulatorStore _store = store;

    public async Task<IReadOnlyList<ReconciledReceivableResponse>> AllocateAsync(
        string merchantCnpj,
        string acquirerCnpj,
        decimal value,
        IReadOnlyList<ScheduledOperationEntity> contracts,
        CancellationToken cancellationToken)
    {
        var entries = await _store.GetEntriesAsync(merchantCnpj, cancellationToken);
        var paid = entries.SelectMany(entry => entry.AllocationsJson.FromJson<List<ReconciledReceivableResponse>>()!)
            .GroupBy(Key)
            .ToDictionary(group => group.Key, group => group.Sum(unit => unit.Amount));

        var candidates = contracts.SelectMany(operation => operation.ToReceivables()
                .Where(unit => unit.AcquirerCnpj == acquirerCnpj)
                .Select(unit => new { Unit = unit, operation.CreatedAt, operation.Id }))
            .OrderBy(candidate => candidate.Unit.SettlementDate, StringComparer.Ordinal)
            .ThenBy(candidate => candidate.CreatedAt)
            .ThenBy(candidate => candidate.Id)
            .ThenBy(candidate => candidate.Unit.HolderCnpj, StringComparer.Ordinal)
            .ThenBy(candidate => candidate.Unit.PaymentArrangementCode, StringComparer.Ordinal);

        var remaining = value;
        var allocations = new List<ReconciledReceivableResponse>();

        foreach (var (unit, amount) in from candidate in candidates
                                       let unit = candidate.Unit
                                       let available = Math.Max(0, unit.Amount - paid.GetValueOrDefault(Key(unit)))
                                       let amount = Math.Min(remaining, available)
                                       select (unit, amount))
        {
            if (amount > 0)
                allocations.Add(unit with { Amount = amount });

            remaining -= amount;

            if (remaining == 0)
                break;
        }

        return allocations;
    }

    private static string Key(ReconciledReceivableResponse unit) => string.Join('|',
        unit.ExternalReference, unit.HolderCnpj, unit.AcquirerCnpj, unit.PaymentArrangementCode, unit.SettlementDate);
}
