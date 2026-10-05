using MVFC.Veragi.Simulator.Domain.Services.Contracts;
using MVFC.Veragi.Simulator.Shareable.Responses.Contracts;
using MediatR;
using OperationResult;

namespace MVFC.Veragi.Simulator.Domain.Handlers.Contracts.CreateAnticipation;

public sealed class CreateAnticipationHandler(ContractService contract) : IRequestHandler<CreateAnticipationCommand, Result<IReadOnlyList<ContractByContractorAndSituation>>>
{
    private readonly ContractService _contract = contract;

    public Task<Result<IReadOnlyList<ContractByContractorAndSituation>>> Handle(CreateAnticipationCommand command, CancellationToken cancellationToken) =>
        _contract.CreateAsync(command.Request, command.IdempotencyKey, cancellationToken);
}
