using MVFC.Veragi.Simulator.Domain.Services.Webhooks;
using MVFC.Veragi.Simulator.Domain.Services.Common;
using MVFC.Veragi.Simulator.Shareable.Responses.Simulation;
using OperationResult;
using MVFC.Veragi.Simulator.Domain.Ports;

namespace MVFC.Veragi.Simulator.Domain.Services.Simulation;

public sealed class EnvironmentService(ISimulatorStore store, SimulationGate gate, DeliveryGate deliveryGate)
{
    private readonly ISimulatorStore _store = store;
    private readonly SimulationGate _gate = gate;
    private readonly DeliveryGate _deliveryGate = deliveryGate;

    public async Task<Result<EnvironmentResetResponse>> ResetAsync(CancellationToken ct)
    {
        await _deliveryGate.EnterAsync(ct);

        try
        {
            using var lease = await _gate.EnterAsync(ct);

            return new EnvironmentResetResponse(await _store.ResetAsync(ct));
        }
        finally
        {
            _deliveryGate.Exit();
        }
    }
}
