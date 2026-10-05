using MVFC.Veragi.Simulator.Domain.Handlers.Webhooks.ReceiveWebhook;
using MVFC.Veragi.Simulator.Shareable.Responses.Webhooks;
using MVFC.Veragi.Simulator.Shareable.Extensions;
using MediatR;
using MVFC.Veragi.Simulator.Api.Extensions;

namespace MVFC.Veragi.Simulator.Api.Endpoints;

public static class SimulationWebhookReceiversEndpoints
{
    public static void MapSimulationWebhookReceivers(this WebApplication app)
    {
        app.MapPost("/webhooks/card-receivables/schedules/updated", async (ScheduleWebhookNotification request, ISender sender, HttpContext http, CancellationToken ct) => (await sender.Send(new ReceiveWebhookCommand("schedule", http.Request.Headers["Idempotency-Key"].ToString(), request.ToJson()), ct)).ToHttp(http, 204, true));
        app.MapPost("/webhooks/card-receivable/schedules", async (ContractWebhookNotification request, ISender sender, HttpContext http, CancellationToken ct) => (await sender.Send(new ReceiveWebhookCommand(http.Request.Headers["X-Simulator-Event-Type"] == "contract" ? "contract" : "schedule", http.Request.Headers["Idempotency-Key"].ToString(), request.ToJson(), http.Request.Headers["X-Contract-External-Reference"].ToString()), ct)).ToHttp(http, 200, true));
    }
}
