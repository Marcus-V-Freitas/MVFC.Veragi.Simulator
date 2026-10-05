using MVFC.Veragi.Simulator.Domain.Services.Simulation;
using MVFC.Veragi.Simulator.Shareable.Responses.Simulation;
using MediatR;

namespace MVFC.Veragi.Simulator.Domain.Handlers.Simulation.ListScenarios;

public sealed class ListScenariosHandler(ScenarioService scenarios) : IRequestHandler<ListScenariosCommand, IReadOnlyList<SimulationScenarioResponse>>
{
    private readonly ScenarioService _scenarios = scenarios;

    public Task<IReadOnlyList<SimulationScenarioResponse>> Handle(ListScenariosCommand command, CancellationToken cancellationToken) =>
        _scenarios.ListAsync(cancellationToken);
}
