using MVFC.Veragi.Simulator.Shareable.Requests.Simulation;
using MVFC.Veragi.Simulator.Shareable.Responses.Simulation;
using MediatR;
using OperationResult;

namespace MVFC.Veragi.Simulator.Domain.Handlers.Simulation.ConfigureScenario;

public sealed record ConfigureScenarioCommand(
    string Target,
    SimulationScenarioRequest Request
) : IRequest<Result<SimulationScenarioResponse>>;
