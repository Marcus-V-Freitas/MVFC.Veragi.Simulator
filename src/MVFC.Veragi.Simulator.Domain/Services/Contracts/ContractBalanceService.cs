using MVFC.Veragi.Simulator.Domain.Services.Receivables;
using MVFC.Veragi.Simulator.Shareable.Requests.Contracts;
using MVFC.Veragi.Simulator.Domain.Ports;

namespace MVFC.Veragi.Simulator.Domain.Services.Contracts;

public sealed class ContractBalanceService(ISimulatorStore store)
{
    private readonly ISimulatorStore _store = store;

    public async Task<Dictionary<string, decimal>> GetReachedAsync(
        ContractAnticipationCreateRequest request,
        Guid? excludedOperation,
        bool includePending,
        CancellationToken cancellationToken
    )
    {
        var contracts = await _store.GetOperationsAsync("contract", request.ContractorCnpj, cancellationToken);
        var commitments = ReceivableBalances.Commitments(contracts.Where(operation => operation.Id != excludedOperation), includePending);
        var externalAnticipations = await _store.GetExternalAnticipationsAsync(request.ContractorCnpj!, cancellationToken);

        foreach (var anticipation in externalAnticipations)
        {
            var key = ReceivableBalances.Key(anticipation.MerchantCnpj, anticipation.AcquirerCnpj, anticipation.PaymentArrangementCode, anticipation.SettlementDate);
            ReceivableBalances.Add(commitments, key, anticipation.Amount);
        }

        var sales = await _store.GetSalesAsync(request.ContractorCnpj!, cancellationToken);
        var totals = sales.GroupBy(sale => ReceivableBalances.Key(sale.MerchantCnpj, sale.AcquirerCnpj, sale.PaymentArrangementCode, sale.SettlementDate)).ToDictionary(group => group.Key, group => group.Sum(sale => sale.Amount));

        return request.Guarantees!.GroupBy(ContractService.UnitKey).ToDictionary(group => group.Key, group => Math.Min(group.Sum(guarantee => guarantee.DefinedAmount ?? 0), Math.Max(0, totals.GetValueOrDefault(group.Key) - commitments.GetValueOrDefault(group.Key))));
    }
}
