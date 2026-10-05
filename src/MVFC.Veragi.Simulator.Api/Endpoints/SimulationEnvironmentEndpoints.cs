using MVFC.Veragi.Simulator.Domain.Handlers.Simulation.ResetEnvironment;
using MediatR;
using MVFC.Veragi.Simulator.Api.Extensions;

namespace MVFC.Veragi.Simulator.Api.Endpoints;

public static class SimulationEnvironmentEndpoints
{
    public static void MapSimulationEnvironment(this RouteGroupBuilder group)
    {
        group.MapPost("/reset", async (ISender sender, HttpContext http, CancellationToken ct) => (await sender.Send(new ResetEnvironmentCommand(), ct)).ToHttp(http));
    }
}
