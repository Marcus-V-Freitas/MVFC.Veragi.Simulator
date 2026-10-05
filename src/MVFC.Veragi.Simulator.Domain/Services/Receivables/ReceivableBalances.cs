using MVFC.Veragi.Simulator.Domain.Services.Contracts;
using MVFC.Veragi.Simulator.Shareable.Requests.Contracts;
using MVFC.Veragi.Simulator.Shareable.Responses.Contracts;
using MVFC.Veragi.Simulator.Domain.Entities;
using MVFC.Veragi.Simulator.Shareable.Enums;
using MVFC.Veragi.Simulator.Shareable.Extensions;

namespace MVFC.Veragi.Simulator.Domain.Services.Receivables;

public static class ReceivableBalances
{
    public static string Key(
        string holder,
        string acquirer,
        string arrangement,
        string date
    ) => string.Join('|', holder, acquirer, arrangement, date);

    public static Dictionary<string, decimal> Commitments(
        IEnumerable<ScheduledOperationEntity> contracts,
        bool includePending
    )
    {
        var result = new Dictionary<string, decimal>();

        foreach (var operation in contracts)
        {
            if (operation.Status == ScheduleQueryStatusType.PROCESSING && includePending)
            {
                foreach (var unit in operation.RequestJson.FromJson<ContractAnticipationCreateRequest>()!.Guarantees!)
                    Add(result, ContractService.UnitKey(unit), unit.DefinedAmount ?? 0);

                continue;
            }

            if (operation.Status != ScheduleQueryStatusType.PROCESSED)
                continue;

            var details = operation.ResultJson.FromJson<ContractByExternalReference>()!;

            if (details.Status is not ContractStatusType.Active and not ContractStatusType.Settled)
                continue;

            foreach (var holder in details.ReachedGuarantees ?? [])
            {
                foreach (var acquirer in holder.Acquirers ?? [])
                {
                    foreach (var arrangement in acquirer.PaymentArrangements ?? [])
                    {
                        foreach (var unit in arrangement.ReceivableUnits ?? [])
                        {
                            Add(result, Key(holder.ReceivableUnitHolderCnpj!, acquirer.Cnpj!, arrangement.Code!, unit.SettlementDate!), unit.ReachedAmount ?? 0);
                        }
                    }
                }
            }
        }

        return result;
    }

    public static void Add(
        Dictionary<string,
        decimal> values,
        string key,
        decimal amount
    ) => values[key] = values.GetValueOrDefault(key) + amount;
}
