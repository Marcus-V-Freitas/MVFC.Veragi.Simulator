using MVFC.Veragi.Simulator.Domain.Handlers.Reconciliation.CreateReconciliation;
using MVFC.Veragi.Simulator.Shareable.Requests.Reconciliation;
using MediatR;
using MVFC.Veragi.Simulator.Api.Extensions;

namespace MVFC.Veragi.Simulator.Api.Endpoints;

public static class ProviderReconciliationEndpoints
{
    public static void MapProviderReconciliation(this RouteGroupBuilder group)
    {
        group.MapPost("/reconciliation/entry", async (BankReconciliationEntry request, ISender sender, HttpContext http, CancellationToken ct) =>
            (await sender.Send(new CreateReconciliationCommand(request), ct)).ToHttp(http, 201, noBody: true)).WithName("BankReconciliationEntryCreate");
    }
}
