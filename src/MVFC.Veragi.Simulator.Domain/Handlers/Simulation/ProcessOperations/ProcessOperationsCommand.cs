using MediatR;
using OperationResult;

namespace MVFC.Veragi.Simulator.Domain.Handlers.Simulation.ProcessOperations;

public sealed record ProcessOperationsCommand(string Kind, bool Force) : IRequest<Result<int>>;
