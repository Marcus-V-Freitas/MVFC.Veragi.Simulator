using MVFC.Veragi.Simulator.Domain.Services.Simulation;
using MVFC.Veragi.Simulator.Shareable.Responses.Simulation;
using MediatR;
using OperationResult;

namespace MVFC.Veragi.Simulator.Domain.Handlers.Simulation.ConfigureScenario;

public sealed class ConfigureScenarioHandler(ScenarioService service) : IRequestHandler<ConfigureScenarioCommand, Result<SimulationScenarioResponse>>
{
    private readonly ScenarioService _service = service;

    public Task<Result<SimulationScenarioResponse>> Handle(ConfigureScenarioCommand request, CancellationToken cancellationToken) =>
        _service.ConfigureAsync(request.Target, request.Request, cancellationToken);
}
