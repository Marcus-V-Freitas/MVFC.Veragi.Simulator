using MVFC.Veragi.Simulator.Domain.Services.Simulation;
using MediatR;
using OperationResult;

namespace MVFC.Veragi.Simulator.Domain.Handlers.Simulation.ClearScenario;

public sealed class ClearScenarioHandler(ScenarioService service) : IRequestHandler<ClearScenarioCommand, Result<bool>>
{
    private readonly ScenarioService _service = service;

    public Task<Result<bool>> Handle(ClearScenarioCommand request, CancellationToken cancellationToken) =>
        _service.ClearAsync(request.Target, request.MerchantCnpj, cancellationToken);
}
