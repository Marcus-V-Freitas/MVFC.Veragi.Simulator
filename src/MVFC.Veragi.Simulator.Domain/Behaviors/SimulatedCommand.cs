using MediatR;
using OperationResult;
using MVFC.Veragi.Simulator.Shareable.Results;

namespace MVFC.Veragi.Simulator.Domain.Behaviors;

public abstract record SimulatedCommand<T> : IRequest<Result<T>>, ISimulatedRequest<Result<T>>
{
    public abstract string Target { get; }
    public virtual string? MerchantCnpj => null;
    public virtual string? SimulationOperationId => null;
    public virtual string? SimulationIdempotencyKey => null;
    public virtual string? SimulationOperationKind => null;

    public Result<T> Reject(SimulationFailureException failure) => failure;
}
