using MediatR;
using OperationResult;

namespace MVFC.Veragi.Simulator.Domain.Handlers.Webhooks.ReceiveWebhook;

public sealed record ReceiveWebhookCommand(
    string Kind,
    string Key,
    string Payload,
    string ExternalReference = ""
) : IRequest<Result<bool>>;
