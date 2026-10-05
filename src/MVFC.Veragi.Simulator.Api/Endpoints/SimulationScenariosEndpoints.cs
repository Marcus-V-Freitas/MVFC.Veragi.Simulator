using MVFC.Veragi.Simulator.Domain.Handlers.Simulation.ClearScenario;
using MVFC.Veragi.Simulator.Domain.Handlers.Simulation.ConfigureScenario;
using MVFC.Veragi.Simulator.Domain.Handlers.Simulation.ListScenarioTargets;
using MVFC.Veragi.Simulator.Domain.Handlers.Simulation.ListScenarios;
using MVFC.Veragi.Simulator.Shareable.Requests.Simulation;
using MediatR;
using MVFC.Veragi.Simulator.Api.Extensions;

namespace MVFC.Veragi.Simulator.Api.Endpoints;

public static class SimulationScenariosEndpoints
{
    public static void MapSimulationScenarios(this RouteGroupBuilder group)
    {
        group.MapPut("/scenarios/{target}", async (string target, SimulationScenarioRequest request, ISender sender, HttpContext http, CancellationToken ct) => (await sender.Send(new ConfigureScenarioCommand(target, request), ct)).ToHttp(http));
        group.MapDelete("/scenarios/{target}", async (string target, string? merchantCnpj, ISender sender, HttpContext http, CancellationToken ct) => (await sender.Send(new ClearScenarioCommand(target, merchantCnpj), ct)).ToHttp(http));
        group.MapGet("/scenarios", async (ISender sender, CancellationToken ct) => Results.Ok(await sender.Send(new ListScenariosCommand(), ct)));
        group.MapGet("/scenario-targets", async (ISender sender, CancellationToken ct) => Results.Ok(await sender.Send(new ListScenarioTargetsCommand(), ct)));
    }
}
