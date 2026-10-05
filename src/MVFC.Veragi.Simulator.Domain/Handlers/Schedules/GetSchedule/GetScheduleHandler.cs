using MVFC.Veragi.Simulator.Domain.Services.Schedules;
using MVFC.Veragi.Simulator.Shareable.Responses.Schedules;
using MediatR;
using OperationResult;

namespace MVFC.Veragi.Simulator.Domain.Handlers.Schedules.GetSchedule;

public sealed class GetScheduleHandler(ScheduleService schedule) : IRequestHandler<GetScheduleCommand, Result<ScheduleQuery>>
{
    private readonly ScheduleService _schedule = schedule;

    public Task<Result<ScheduleQuery>> Handle(GetScheduleCommand command, CancellationToken cancellationToken) =>
        _schedule.GetAsync(command.Uuid, cancellationToken);
}
