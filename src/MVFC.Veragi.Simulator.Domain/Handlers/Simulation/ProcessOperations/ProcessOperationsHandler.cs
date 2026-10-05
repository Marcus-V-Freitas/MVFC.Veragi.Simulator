using MVFC.Veragi.Simulator.Domain.Services.Simulation;
using MediatR;
using OperationResult;

namespace MVFC.Veragi.Simulator.Domain.Handlers.Simulation.ProcessOperations;

public sealed class ProcessOperationsHandler(OperationProcessor processor) : IRequestHandler<ProcessOperationsCommand, Result<int>>
{
    private readonly OperationProcessor _processor = processor;

    public Task<Result<int>> Handle(ProcessOperationsCommand command, CancellationToken cancellationToken) =>
        _processor.ProcessAsync(command.Kind, command.Force, cancellationToken);
}
