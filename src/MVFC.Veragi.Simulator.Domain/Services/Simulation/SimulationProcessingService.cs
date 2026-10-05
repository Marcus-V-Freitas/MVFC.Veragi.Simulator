using MVFC.Veragi.Simulator.Domain.Services.Webhooks;
using MVFC.Veragi.Simulator.Shareable.Responses.Simulation;
using OperationResult;

namespace MVFC.Veragi.Simulator.Domain.Services.Simulation;

public sealed class SimulationProcessingService(OperationProcessor processor, WebhookDispatcher dispatcher)
{
    private readonly OperationProcessor _processor = processor;
    private readonly WebhookDispatcher _dispatcher = dispatcher;

    public async Task<Result<SimulationRunResponse>> RunAsync(CancellationToken cancellationToken)
    {
        var schedules = await _processor.ProcessAsync("schedule", force: true, cancellationToken);
        var contracts = await _processor.ProcessAsync("contract", force: true, cancellationToken);
        var deliveries = await _dispatcher.DispatchAsync(cancellationToken);

        return new SimulationRunResponse(schedules.Value, contracts.Value, deliveries.Value);
    }
}
