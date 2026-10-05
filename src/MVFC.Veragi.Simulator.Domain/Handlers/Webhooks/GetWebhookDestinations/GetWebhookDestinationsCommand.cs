using MVFC.Veragi.Simulator.Shareable.Responses.Webhooks;
using MediatR;
using OperationResult;

namespace MVFC.Veragi.Simulator.Domain.Handlers.Webhooks.GetWebhookDestinations;

public sealed record GetWebhookDestinationsCommand() : IRequest<Result<WebhookDestinationsResponse>>;
