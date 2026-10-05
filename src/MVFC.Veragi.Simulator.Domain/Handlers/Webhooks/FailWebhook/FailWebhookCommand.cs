using MediatR;
using OperationResult;

namespace MVFC.Veragi.Simulator.Domain.Handlers.Webhooks.FailWebhook;

public sealed record FailWebhookCommand() : IRequest<int>;
