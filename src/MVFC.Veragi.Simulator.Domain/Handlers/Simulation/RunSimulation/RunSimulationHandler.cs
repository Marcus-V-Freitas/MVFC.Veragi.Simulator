using MVFC.Veragi.Simulator.Domain.Services.Simulation;
using MVFC.Veragi.Simulator.Shareable.Responses.Simulation;
using MediatR;
using OperationResult;

namespace MVFC.Veragi.Simulator.Domain.Handlers.Simulation.RunSimulation;

public sealed class RunSimulationHandler(SimulationProcessingService processing) : IRequestHandler<RunSimulationCommand, Result<SimulationRunResponse>>
{
    private readonly SimulationProcessingService _processing = processing;

    public Task<Result<SimulationRunResponse>> Handle(RunSimulationCommand command, CancellationToken cancellationToken) =>
        _processing.RunAsync(cancellationToken);
}
