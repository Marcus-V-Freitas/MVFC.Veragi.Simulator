using MVFC.Veragi.Simulator.Shareable.Responses.Schedules;
using MVFC.Veragi.Simulator.Domain.Behaviors;
using MediatR;
using OperationResult;

namespace MVFC.Veragi.Simulator.Domain.Handlers.Schedules.GetSchedule;

public sealed record GetScheduleCommand(string Uuid) : SimulatedCommand<ScheduleQuery>
{
    public override string Target => "schedule-query";
    public override string? MerchantCnpj => null;
    public override string? SimulationOperationId => Uuid;
}
