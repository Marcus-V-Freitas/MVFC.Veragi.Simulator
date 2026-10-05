using MVFC.Veragi.Simulator.Domain.Services.Webhooks;
using MVFC.Veragi.Simulator.Domain.Services.Common;
using MVFC.Veragi.Simulator.Domain.Mappings;
using OperationResult;
using MVFC.Veragi.Simulator.Domain.Ports;
using MVFC.Veragi.Simulator.Shareable.Results;

namespace MVFC.Veragi.Simulator.Domain.Services.Simulation;

public sealed class ControlService(
    ISimulatorStore store,
    SimulationGate gate,
    DeliveryGate deliveryGate,
    TimeProvider clock
)
{
    private readonly ISimulatorStore _store = store;
    private readonly SimulationGate _gate = gate;
    private readonly DeliveryGate _deliveryGate = deliveryGate;
    private readonly TimeProvider _clock = clock;

    public async Task<Result<bool>> ReplayAsync(string id, CancellationToken cancellationToken)
    {
        await _deliveryGate.EnterAsync(cancellationToken);

        try
        {
            var delivery = await _store.GetDeliveryAsync(id, cancellationToken);

            if (delivery is null)
                return Failures.Missing("Event not found");

            delivery.Delivered = false;
            delivery.DeadLetter = false;
            delivery.Attempts = 0;
            delivery.NextAttemptAt = _clock.GetUtcNow().UtcDateTime;
            await _store.SaveAsync(cancellationToken);

            return true;
        }
        finally
        {
            _deliveryGate.Exit();
        }
    }

    public async Task<Result<bool>> ReceiveAsync(
        string kind,
        string key,
        string payload,
        CancellationToken cancellationToken,
        string externalReference = ""
    )
    {
        using var lease = await _gate.EnterAsync(cancellationToken);

        if (!Guid.TryParse(key, out var id))
            return Failures.Validation("Invalid Idempotency-Key");

        var receiptId = kind + ":" + id;
        var existing = await _store.GetReceiptAsync(receiptId, cancellationToken);

        if (existing is not null)
            return existing.Payload == payload ? Result.Success(value: true) : Failures.Conflict("Event key reused with different payload");

        _store.AddReceipt(payload.ToReceipt(kind, receiptId, externalReference, _clock.GetUtcNow().UtcDateTime));
        await _store.SaveAsync(cancellationToken);

        return true;
    }
}
