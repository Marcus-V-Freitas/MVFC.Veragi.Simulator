using MVFC.Veragi.Simulator.Domain.Handlers.Webhooks.ConfigureWebhookDestinations;
using MVFC.Veragi.Simulator.Domain.Handlers.Webhooks.FailWebhook;
using MVFC.Veragi.Simulator.Domain.Handlers.Webhooks.GetWebhookDestinations;
using MVFC.Veragi.Simulator.Domain.Handlers.Webhooks.ListDeliveries;
using MVFC.Veragi.Simulator.Domain.Handlers.Webhooks.ListReceipts;
using MVFC.Veragi.Simulator.Domain.Handlers.Webhooks.ReplayDelivery;
using MVFC.Veragi.Simulator.Shareable.Requests.Webhooks;
using MediatR;
using MVFC.Veragi.Simulator.Api.Extensions;

namespace MVFC.Veragi.Simulator.Api.Endpoints;

public static class SimulationWebhooksEndpoints
{
    public static void MapSimulationWebhooks(this RouteGroupBuilder group)
    {
        group.MapPut("/webhooks", async (WebhookDestinationsRequest request, ISender sender, HttpContext http, CancellationToken ct) => (await sender.Send(new ConfigureWebhookDestinationsCommand(request), ct)).ToHttp(http));
        group.MapGet("/webhooks", async (ISender sender, HttpContext http, CancellationToken ct) => (await sender.Send(new GetWebhookDestinationsCommand(), ct)).ToHttp(http));
        group.MapGet("/deliveries", async (string? kind, string? merchantCnpj, ISender sender, CancellationToken ct) => Results.Ok(await sender.Send(new ListDeliveriesCommand(kind, merchantCnpj), ct)));
        group.MapGet("/receipts", async (string? kind, string? merchantCnpj, string? requestId, ISender sender, CancellationToken ct) => Results.Ok(await sender.Send(new ListReceiptsCommand(kind, merchantCnpj, requestId), ct)));
        group.MapPost("/deliveries/{id}/replay", async (string id, ISender sender, HttpContext http, CancellationToken ct) => (await sender.Send(new ReplayDeliveryCommand(id), ct)).ToHttp(http));
        group.MapPost("/webhooks/fail", async (ISender sender, CancellationToken ct) => Results.StatusCode(await sender.Send(new FailWebhookCommand(), ct)));
    }
}
