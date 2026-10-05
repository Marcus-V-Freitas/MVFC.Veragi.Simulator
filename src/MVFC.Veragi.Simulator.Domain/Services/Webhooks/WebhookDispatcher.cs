using MVFC.Veragi.Simulator.Domain.Services.Simulation;
using OperationResult;
using MVFC.Veragi.Simulator.Domain.Ports;
using MVFC.Veragi.Simulator.Shareable.Configuration;
using MVFC.Veragi.Simulator.Shareable.Results;

namespace MVFC.Veragi.Simulator.Domain.Services.Webhooks;

public sealed class WebhookDispatcher(
    ISimulatorStore store,
    IWebhookSender sender,
    DeliveryGate gate,
    SimulatorOptions options,
    TimeProvider clock,
    ScenarioService scenarios,
    WebhookRoutingService routing
)
{
    private readonly ISimulatorStore _store = store;
    private readonly IWebhookSender _sender = sender;
    private readonly DeliveryGate _gate = gate;
    private readonly SimulatorOptions _options = options;
    private readonly TimeProvider _clock = clock;
    private readonly ScenarioService _scenarios = scenarios;
    private readonly WebhookRoutingService _routing = routing;

    public async Task<Result<int>> DispatchAsync(CancellationToken cancellationToken)
    {
        await _gate.EnterAsync(cancellationToken);

        try
        {
            var deliveries = await _store.GetPendingDeliveriesAsync(_clock.GetUtcNow().UtcDateTime, cancellationToken);
            var sent = 0;

            foreach (var delivery in deliveries)
            {
                var configuredFailure = await _scenarios.TakeFailureAsync(delivery.Kind + "-webhook", delivery.MerchantCnpj, cancellationToken);
                Result<int> result = configuredFailure is null ? await _sender.SendAsync(delivery.Kind, delivery.Id.ToString(), delivery.Payload, delivery.ExternalReference, await _routing.ResolveAsync(delivery.Kind, cancellationToken), cancellationToken) : new SimulationFailureException(configuredFailure.Value, "SIMULATED_WEBHOOK_FAILURE", "Configured webhook delivery failure");
                delivery.Attempts++;
                if (result.IsSuccess)
                {
                    delivery.LastHttpStatus = result.Value;
                }
                else if (result.Exception is SimulationFailureException failure && failure.StatusCode > 0)
                {
                    delivery.LastHttpStatus = failure.StatusCode;
                }
                else
                {
                    delivery.LastHttpStatus = null;
                }
                delivery.Delivered = result.IsSuccess;
                delivery.DeadLetter = !result.IsSuccess && delivery.Attempts >= _options.MaxDeliveryAttempts;
                delivery.NextAttemptAt = _clock.GetUtcNow().UtcDateTime.AddSeconds(Math.Min(300, _options.RetryBaseSeconds * Math.Pow(2, delivery.Attempts - 1)));
                await _store.SaveAsync(cancellationToken);

                if (delivery.Delivered)
                    sent++;
            }

            return sent;
        }
        finally
        {
            _gate.Exit();
        }
    }
}
