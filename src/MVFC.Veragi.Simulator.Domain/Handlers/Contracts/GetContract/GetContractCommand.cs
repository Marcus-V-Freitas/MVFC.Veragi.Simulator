using MVFC.Veragi.Simulator.Shareable.Responses.Contracts;
using MVFC.Veragi.Simulator.Domain.Behaviors;
using MediatR;
using OperationResult;

namespace MVFC.Veragi.Simulator.Domain.Handlers.Contracts.GetContract;

public sealed record GetContractCommand(
    string ContractorCnpj,
    string ExternalReference
) : SimulatedCommand<ContractByExternalReference>
{
    public override string Target => "contract-query";
    public override string? MerchantCnpj => ContractorCnpj;
}
