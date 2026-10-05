using MVFC.Veragi.Simulator.Domain.Services.Contracts;
using MVFC.Veragi.Simulator.Shareable.Responses.Contracts;
using MediatR;
using OperationResult;

namespace MVFC.Veragi.Simulator.Domain.Handlers.Simulation.GetContractAvailability;

public sealed class GetContractAvailabilityHandler(ContractAvailabilityService availability) : IRequestHandler<GetContractAvailabilityCommand, Result<ContractAvailabilityResponse>>
{
    private readonly ContractAvailabilityService _availability = availability;

    public Task<Result<ContractAvailabilityResponse>> Handle(GetContractAvailabilityCommand command, CancellationToken cancellationToken) =>
        _availability.GetAsync(cancellationToken);
}
