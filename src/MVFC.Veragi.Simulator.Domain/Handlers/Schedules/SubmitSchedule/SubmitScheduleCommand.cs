using MVFC.Veragi.Simulator.Shareable.Requests.Schedules;
using MVFC.Veragi.Simulator.Shareable.Responses.Schedules;
using MVFC.Veragi.Simulator.Domain.Behaviors;
using MediatR;
using OperationResult;

namespace MVFC.Veragi.Simulator.Domain.Handlers.Schedules.SubmitSchedule;

public sealed record SubmitScheduleCommand(
    ScheduleQueryRequest Request,
    string IdempotencyKey
) : SimulatedCommand<ScheduleQueryResponse>
{
    public override string Target => "schedule-submit";
    public override string? MerchantCnpj => Request.MerchantCnpj;
    public override string? SimulationIdempotencyKey => IdempotencyKey;
    public override string? SimulationOperationKind => "schedule";
}
