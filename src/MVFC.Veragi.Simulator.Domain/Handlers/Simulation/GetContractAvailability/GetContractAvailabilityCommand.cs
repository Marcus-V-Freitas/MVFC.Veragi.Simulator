using MVFC.Veragi.Simulator.Shareable.Responses.Contracts;
using MediatR;
using OperationResult;

namespace MVFC.Veragi.Simulator.Domain.Handlers.Simulation.GetContractAvailability;

public sealed record GetContractAvailabilityCommand() : IRequest<Result<ContractAvailabilityResponse>>;
