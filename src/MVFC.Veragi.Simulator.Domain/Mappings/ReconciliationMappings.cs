using MVFC.Veragi.Simulator.Domain.Entities;
using MVFC.Veragi.Simulator.Shareable.Extensions;
using MVFC.Veragi.Simulator.Shareable.Responses.Contracts;
using MVFC.Veragi.Simulator.Shareable.Responses.Reconciliation;

namespace MVFC.Veragi.Simulator.Domain.Mappings;

public static class ReconciliationMappings
{
    public static IReadOnlyList<ReconciledReceivableResponse> ToReceivables(this ScheduledOperationEntity operation)
    {
        var details = operation.ResultJson.FromJson<ContractByExternalReference>()!;
        var receivables = new List<ReconciledReceivableResponse>();

        foreach (var holder in details.ReachedGuarantees ?? [])
        {
            foreach (var acquirer in holder.Acquirers ?? [])
            {
                foreach (var arrangement in acquirer.PaymentArrangements ?? [])
                {
                    foreach (var unit in arrangement.ReceivableUnits ?? [])
                    {
                        receivables.Add(new ReconciledReceivableResponse(
                            operation.ExternalReference,
                            holder.ReceivableUnitHolderCnpj!,
                            acquirer.Cnpj!,
                            arrangement.Code!,
                            unit.SettlementDate!,
                            unit.ReachedAmount ?? 0));
                    }
                }
            }
        }

        return receivables;
    }
}
