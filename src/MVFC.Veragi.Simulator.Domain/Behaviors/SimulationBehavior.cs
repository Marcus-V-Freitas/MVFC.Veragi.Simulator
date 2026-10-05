using MVFC.Veragi.Simulator.Domain.Services.Simulation;
using MediatR;
using MVFC.Veragi.Simulator.Domain.Ports;
using MVFC.Veragi.Simulator.Shareable.Results;

namespace MVFC.Veragi.Simulator.Domain.Behaviors;

public sealed class SimulationBehavior<TRequest, TResponse>(
    ISimulatorStore store,
    ScenarioService scenarios
) : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    private readonly ISimulatorStore _store = store;
    private readonly ScenarioService _scenarios = scenarios;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken
    )
    {
        if (request is not ISimulatedRequest<TResponse> simulated)
            return await next(cancellationToken);

        var merchantCnpj = simulated.MerchantCnpj;

        if (merchantCnpj is null && Guid.TryParse(simulated.SimulationOperationId, out var operationId))
            merchantCnpj = (await _store.GetOperationAsync(operationId, cancellationToken))?.MerchantCnpj;

        var idempotencyKey = simulated.SimulationIdempotencyKey;

        if (simulated.SimulationOperationKind is not null && Guid.TryParse(idempotencyKey, out var key))
        {
            var scope = simulated.SimulationOperationKind == "schedule" ? merchantCnpj : null;
            var operations = await _store.GetOperationsAsync(simulated.SimulationOperationKind, scope, cancellationToken);

            if (operations.Any(operation => operation.IdempotencyKey == key.ToString()))
                return await next(cancellationToken);
        }

        var failure = await _scenarios.TakeFailureAsync(simulated.Target, merchantCnpj, cancellationToken);

        return failure is null ? await next(cancellationToken) : simulated.Reject(new SimulationFailureException(failure.Value, "SIMULATED_FAILURE", "Configured failure for " + simulated.Target));
    }
}
