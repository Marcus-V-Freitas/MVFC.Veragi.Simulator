using MVFC.Veragi.Simulator.Domain.Services.Schedules;
using MVFC.Veragi.Simulator.Shareable.Responses.Schedules;
using MediatR;
using OperationResult;

namespace MVFC.Veragi.Simulator.Domain.Handlers.Schedules.SubmitSchedule;

public sealed class SubmitScheduleHandler(ScheduleService schedule) : IRequestHandler<SubmitScheduleCommand, Result<ScheduleQueryResponse>>
{
    private readonly ScheduleService _schedule = schedule;

    public Task<Result<ScheduleQueryResponse>> Handle(SubmitScheduleCommand command, CancellationToken cancellationToken) =>
        _schedule.SubmitAsync(command.Request, command.IdempotencyKey, cancellationToken);
}
