using MediatR;
using OperationResult;

namespace MVFC.Veragi.Simulator.Domain.Handlers.Webhooks.ReplayDelivery;

public sealed record ReplayDeliveryCommand(string Id) : IRequest<Result<bool>>;
