using MVFC.Veragi.Simulator.Domain.Handlers.Schedules.RefreshSchedule;
using MVFC.Veragi.Simulator.Domain.Handlers.Simulation.RunSimulation;
using MediatR;
using MVFC.Veragi.Simulator.Api.Extensions;

namespace MVFC.Veragi.Simulator.Api.Endpoints;

public static class SimulationProcessingEndpoints
{
    public static void MapSimulationProcessing(this RouteGroupBuilder group)
    {
        group.MapPost("/run", async (ISender sender, HttpContext http, CancellationToken ct) => (await sender.Send(new RunSimulationCommand(), ct)).ToHttp(http));
        group.MapPost("/schedules/{id:guid}/refresh", async (Guid id, ISender sender, HttpContext http, CancellationToken ct) => (await sender.Send(new RefreshScheduleCommand(id), ct)).ToHttp(http));
    }
}
