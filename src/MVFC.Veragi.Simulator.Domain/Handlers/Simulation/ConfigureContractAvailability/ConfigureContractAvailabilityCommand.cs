using MVFC.Veragi.Simulator.Shareable.Requests.Contracts;
using MVFC.Veragi.Simulator.Shareable.Responses.Contracts;
using MediatR;
using OperationResult;

namespace MVFC.Veragi.Simulator.Domain.Handlers.Simulation.ConfigureContractAvailability;

public sealed record ConfigureContractAvailabilityCommand(ContractAvailabilityRequest Request) : IRequest<Result<ContractAvailabilityResponse>>;
