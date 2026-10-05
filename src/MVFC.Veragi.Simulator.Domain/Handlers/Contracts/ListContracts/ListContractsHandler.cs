using MVFC.Veragi.Simulator.Domain.Services.Contracts;
using MVFC.Veragi.Simulator.Shareable.Responses.Contracts;
using MediatR;
using OperationResult;

namespace MVFC.Veragi.Simulator.Domain.Handlers.Contracts.ListContracts;

public sealed class ListContractsHandler(ContractService contract) : IRequestHandler<ListContractsCommand, Result<IReadOnlyList<ContractByContractorAndSituation>>>
{
    private readonly ContractService _contract = contract;

    public Task<Result<IReadOnlyList<ContractByContractorAndSituation>>> Handle(ListContractsCommand command, CancellationToken cancellationToken) =>
        _contract.ListAsync(command.ContractorCnpj, command.Situation, cancellationToken);
}
