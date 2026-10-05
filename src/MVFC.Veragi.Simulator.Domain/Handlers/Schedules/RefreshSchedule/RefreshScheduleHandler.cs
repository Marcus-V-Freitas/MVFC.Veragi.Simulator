using MVFC.Veragi.Simulator.Domain.Services.Simulation;
using MediatR;
using OperationResult;

namespace MVFC.Veragi.Simulator.Domain.Handlers.Schedules.RefreshSchedule;

public sealed class RefreshScheduleHandler(OperationProcessor processor) : IRequestHandler<RefreshScheduleCommand, Result<bool>>
{
    private readonly OperationProcessor _processor = processor;

    public Task<Result<bool>> Handle(RefreshScheduleCommand command, CancellationToken cancellationToken) =>
        _processor.RepublishScheduleAsync(command.Id, cancellationToken);
}
