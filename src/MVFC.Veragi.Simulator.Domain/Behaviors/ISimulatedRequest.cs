using MVFC.Veragi.Simulator.Shareable.Results;

namespace MVFC.Veragi.Simulator.Domain.Behaviors;

public interface ISimulatedRequest<out TResponse>
{
    string Target { get; }

    string? MerchantCnpj { get; }

    string? SimulationOperationId { get; }

    string? SimulationIdempotencyKey { get; }

    string? SimulationOperationKind { get; }

    TResponse Reject(SimulationFailureException failure);
}
