using MVFC.Veragi.Simulator.Domain.Services.Common;
using MVFC.Veragi.Simulator.Domain.Mappings;
using MVFC.Veragi.Simulator.Shareable.Requests.Webhooks;
using MVFC.Veragi.Simulator.Shareable.Responses.Webhooks;
using OperationResult;
using MVFC.Veragi.Simulator.Domain.Entities;
using MVFC.Veragi.Simulator.Domain.Ports;
using MVFC.Veragi.Simulator.Shareable.Configuration;
using MVFC.Veragi.Simulator.Shareable.Results;

namespace MVFC.Veragi.Simulator.Domain.Services.Webhooks;

public sealed class WebhookRoutingService(
    ISimulatorStore store,
    SimulationGate gate,
    DeliveryGate deliveryGate
)
{
    private readonly ISimulatorStore _store = store;
    private readonly SimulationGate _gate = gate;
    private readonly DeliveryGate _deliveryGate = deliveryGate;

    public async Task<Result<WebhookDestinationsResponse>> ConfigureAsync(
        WebhookDestinationsRequest request,
        CancellationToken ct
    )
    {
        if (!ValidUrl(request.ScheduleUrl) || !ValidUrl(request.ContractUrl))
            return Failures.Validation("Both webhook URLs must be absolute HTTP or HTTPS URLs without credentials");

        await _deliveryGate.EnterAsync(ct);

        try
        {
            using var lease = await _gate.EnterAsync(ct);
            var routing = await _store.GetWebhookRoutingAsync(ct);

            if (routing is null)
            {
                routing = new WebhookRoutingEntity();
                _store.AddWebhookRouting(routing);
            }

            routing.ScheduleUrl = request.ScheduleUrl!;
            routing.ContractUrl = request.ContractUrl!;
            await _store.SaveAsync(ct);

            return routing.ToResponse();
        }
        finally
        {
            _deliveryGate.Exit();
        }
    }

    public async Task<Result<WebhookDestinationsResponse>> GetAsync(CancellationToken ct) =>
        (await _store.GetWebhookRoutingAsync(ct)).ToResponse();

    public async Task<string> ResolveAsync(string kind, CancellationToken ct)
    {
        var routing = await _store.GetWebhookRoutingAsync(ct);
        var url = kind == "schedule" ? routing?.ScheduleUrl : routing?.ContractUrl;

        return url ?? string.Empty;
    }

    private static bool ValidUrl(string? value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https" && string.IsNullOrEmpty(uri.UserInfo);
}
