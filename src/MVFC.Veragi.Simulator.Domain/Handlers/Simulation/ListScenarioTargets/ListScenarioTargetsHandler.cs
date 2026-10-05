using MVFC.Veragi.Simulator.Domain.Services.Simulation;
using MediatR;

namespace MVFC.Veragi.Simulator.Domain.Handlers.Simulation.ListScenarioTargets;

public sealed class ListScenarioTargetsHandler : IRequestHandler<ListScenarioTargetsCommand, IReadOnlyList<string>>
{
    public Task<IReadOnlyList<string>> Handle(ListScenarioTargetsCommand command, CancellationToken cancellationToken) =>
        Task.FromResult(ScenarioService.Targets);
}
