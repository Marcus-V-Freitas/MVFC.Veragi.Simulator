using MVFC.Veragi.Simulator.Domain.Services.Contracts;
using MVFC.Veragi.Simulator.Shareable.Responses.Contracts;
using MediatR;
using OperationResult;

namespace MVFC.Veragi.Simulator.Domain.Handlers.Contracts.GetContract;

public sealed class GetContractHandler(ContractService contract) : IRequestHandler<GetContractCommand, Result<ContractByExternalReference>>
{
    private readonly ContractService _contract = contract;

    public Task<Result<ContractByExternalReference>> Handle(GetContractCommand command, CancellationToken cancellationToken) =>
        _contract.GetAsync(command.ContractorCnpj, command.ExternalReference, cancellationToken);
}
