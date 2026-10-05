using MediatR;
using OperationResult;

namespace MVFC.Veragi.Simulator.Domain.Handlers.Simulation.ListScenarioTargets;

public sealed record ListScenarioTargetsCommand() : IRequest<IReadOnlyList<string>>;
