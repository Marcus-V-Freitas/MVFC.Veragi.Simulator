using MVFC.Veragi.Simulator.Shareable.Responses.Contracts;
using MVFC.Veragi.Simulator.Domain.Behaviors;
using MediatR;
using OperationResult;

namespace MVFC.Veragi.Simulator.Domain.Handlers.Contracts.ListContracts;

public sealed record ListContractsCommand(
    string ContractorCnpj,
    string? Situation
) : SimulatedCommand<IReadOnlyList<ContractByContractorAndSituation>>
{
    public override string Target => "contract-list";
    public override string? MerchantCnpj => ContractorCnpj;
}
