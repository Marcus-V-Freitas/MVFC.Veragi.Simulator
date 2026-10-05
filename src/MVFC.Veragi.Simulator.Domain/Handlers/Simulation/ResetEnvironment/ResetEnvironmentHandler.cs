using MVFC.Veragi.Simulator.Domain.Services.Simulation;
using MVFC.Veragi.Simulator.Shareable.Responses.Simulation;
using MediatR;
using OperationResult;

namespace MVFC.Veragi.Simulator.Domain.Handlers.Simulation.ResetEnvironment;

public sealed class ResetEnvironmentHandler(EnvironmentService service) : IRequestHandler<ResetEnvironmentCommand, Result<EnvironmentResetResponse>>
{
    private readonly EnvironmentService _service = service;

    public Task<Result<EnvironmentResetResponse>> Handle(ResetEnvironmentCommand request, CancellationToken cancellationToken) =>
        _service.ResetAsync(cancellationToken);
}
