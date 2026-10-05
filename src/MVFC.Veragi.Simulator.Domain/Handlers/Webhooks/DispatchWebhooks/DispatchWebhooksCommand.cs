using MediatR;
using OperationResult;

namespace MVFC.Veragi.Simulator.Domain.Handlers.Webhooks.DispatchWebhooks;

public sealed record DispatchWebhooksCommand() : IRequest<Result<int>>;
