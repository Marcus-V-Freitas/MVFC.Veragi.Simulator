using MVFC.Veragi.Simulator.Domain.Handlers.Simulation.ConfigureContractAvailability;
using MVFC.Veragi.Simulator.Domain.Handlers.Simulation.GetContractAvailability;
using MVFC.Veragi.Simulator.Shareable.Requests.Contracts;
using MediatR;
using MVFC.Veragi.Simulator.Api.Extensions;

namespace MVFC.Veragi.Simulator.Api.Endpoints;

public static class SimulationContractAvailabilityEndpoints
{
    public static void MapSimulationContractAvailability(this RouteGroupBuilder group)
    {
        group.MapGet("/contracts/availability", async (ISender sender, HttpContext http, CancellationToken ct) => (await sender.Send(new GetContractAvailabilityCommand(), ct)).ToHttp(http));
        group.MapPut("/contracts/availability", async (ContractAvailabilityRequest request, ISender sender, HttpContext http, CancellationToken ct) => (await sender.Send(new ConfigureContractAvailabilityCommand(request), ct)).ToHttp(http));
    }
}
