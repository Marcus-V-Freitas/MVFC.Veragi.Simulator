using MediatR;
using OperationResult;

namespace MVFC.Veragi.Simulator.Domain.Handlers.Schedules.RefreshSchedule;

public sealed record RefreshScheduleCommand(Guid Id) : IRequest<Result<bool>>;
