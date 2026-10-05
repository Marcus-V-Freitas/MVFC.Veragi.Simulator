using MVFC.Veragi.Simulator.Domain.Handlers.Reconciliation.CreateExternalAnticipation;
using MVFC.Veragi.Simulator.Domain.Handlers.Reconciliation.ListReconciliationEntries;
using MVFC.Veragi.Simulator.Shareable.Requests.Reconciliation;
using MediatR;
using MVFC.Veragi.Simulator.Api.Extensions;

namespace MVFC.Veragi.Simulator.Api.Endpoints;

public static class SimulationReconciliationEndpoints
{
    public static void MapSimulationReconciliation(this RouteGroupBuilder group)
    {
        group.MapPost("/merchants/{cnpj}/external-anticipations", async (string cnpj, ExternalAnticipationRequest request, ISender sender, HttpContext http, CancellationToken ct) => (await sender.Send(new CreateExternalAnticipationCommand(cnpj, request, http.Request.Headers["Idempotency-Key"].ToString()), ct)).ToHttp(http, 201));
        group.MapGet("/reconciliation/entries", async (string? merchantCnpj, ISender sender, CancellationToken ct) => Results.Ok(await sender.Send(new ListReconciliationEntriesCommand(merchantCnpj), ct)));
    }
}
