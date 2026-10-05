using MVFC.Veragi.Simulator.Shareable.Requests.Contracts;
using MVFC.Veragi.Simulator.Shareable.Responses.Contracts;
using MVFC.Veragi.Simulator.Domain.Entities;
using MVFC.Veragi.Simulator.Domain.Mappings;
using MVFC.Veragi.Simulator.Shareable.Enums;
using MVFC.Veragi.Simulator.Shareable.Extensions;

namespace MVFC.Veragi.Simulator.Domain.Services.Contracts;

public sealed class ContractRegistrationService(ContractBalanceService balances)
{
    private readonly ContractBalanceService _balances = balances;

    public async Task<ContractByExternalReference> RegisterAsync(
        ScheduledOperationEntity operation,
        ContractStatusType status,
        CancellationToken ct
    )
    {
        var request = operation.RequestJson.FromJson<ContractAnticipationCreateRequest>()!;
        var persisted = operation.ResultJson.FromJson<ContractByExternalReference>()!;
        request = request with { FinancierContractId = request.FinancierContractId ?? persisted.FinancierContractId ?? operation.ExternalReference };

        if (status is not ContractStatusType.Active and not ContractStatusType.Settled)
            return request.ToDetails(operation.ExternalReference, status);

        var reached = await _balances.GetReachedAsync(request, operation.Id, includePending: false, ct);

        if (reached.Values.Sum() == 0)
            status = ContractStatusType.Cancelled;

        return request.ToDetails(operation.ExternalReference, status, reached);
    }
}
