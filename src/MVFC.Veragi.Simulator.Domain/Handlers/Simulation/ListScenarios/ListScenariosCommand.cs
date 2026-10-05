using MVFC.Veragi.Simulator.Shareable.Responses.Simulation;
using MediatR;
using OperationResult;

namespace MVFC.Veragi.Simulator.Domain.Handlers.Simulation.ListScenarios;

public sealed record ListScenariosCommand() : IRequest<IReadOnlyList<SimulationScenarioResponse>>;
