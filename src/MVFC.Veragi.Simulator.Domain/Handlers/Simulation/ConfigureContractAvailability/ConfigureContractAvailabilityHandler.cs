using MVFC.Veragi.Simulator.Domain.Services.Contracts;
using MVFC.Veragi.Simulator.Shareable.Responses.Contracts;
using MediatR;
using OperationResult;

namespace MVFC.Veragi.Simulator.Domain.Handlers.Simulation.ConfigureContractAvailability;

public sealed class ConfigureContractAvailabilityHandler(ContractAvailabilityService availability) : IRequestHandler<ConfigureContractAvailabilityCommand, Result<ContractAvailabilityResponse>>
{
    private readonly ContractAvailabilityService _availability = availability;

    public Task<Result<ContractAvailabilityResponse>> Handle(ConfigureContractAvailabilityCommand command, CancellationToken cancellationToken) =>
        _availability.ConfigureAsync(command.Request, cancellationToken);
}
