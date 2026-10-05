using MVFC.Veragi.Simulator.Domain.Handlers.Schedules.GetSchedule;
using MVFC.Veragi.Simulator.Domain.Handlers.Schedules.SubmitSchedule;
using MVFC.Veragi.Simulator.Shareable.Requests.Schedules;
using MediatR;
using MVFC.Veragi.Simulator.Api.Extensions;

namespace MVFC.Veragi.Simulator.Api.Endpoints;

public static class ProviderSchedulesEndpoints
{
    public static void MapProviderSchedules(this RouteGroupBuilder group)
    {
        group.MapPost("/schedules/query-requests", async (ScheduleQueryRequest request, ISender sender, HttpContext http, CancellationToken ct) => (await sender.Send(new SubmitScheduleCommand(request, http.Request.Headers["Idempotency-Key"].ToString()), ct)).ToHttp(http, 202)).WithName("ScheduleQueryRequestSubmit");
        group.MapGet("/schedules/query-requests/{uuid}", async (string uuid, ISender sender, HttpContext http, CancellationToken ct) => (await sender.Send(new GetScheduleCommand(uuid), ct)).ToHttp(http)).WithName("ScheduleQueryGetByUuid");
    }
}
