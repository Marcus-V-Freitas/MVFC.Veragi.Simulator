using MVFC.Veragi.Simulator.Shareable.Requests.Contracts;
using MVFC.Veragi.Simulator.Shareable.Responses.Contracts;
using MVFC.Veragi.Simulator.Domain.Behaviors;
using MediatR;
using OperationResult;

namespace MVFC.Veragi.Simulator.Domain.Handlers.Contracts.CreateAnticipation;

public sealed record CreateAnticipationCommand(
    ContractAnticipationCreateRequest Request,
    string IdempotencyKey
) : SimulatedCommand<IReadOnlyList<ContractByContractorAndSituation>>
{
    public override string Target => "contract-create";
    public override string? MerchantCnpj => Request.ContractorCnpj;
    public override string? SimulationIdempotencyKey => IdempotencyKey;
    public override string? SimulationOperationKind => "contract";
}
