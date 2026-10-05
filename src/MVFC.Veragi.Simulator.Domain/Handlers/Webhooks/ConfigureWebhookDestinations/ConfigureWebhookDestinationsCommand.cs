using MVFC.Veragi.Simulator.Shareable.Requests.Webhooks;
using MVFC.Veragi.Simulator.Shareable.Responses.Webhooks;
using MediatR;
using OperationResult;

namespace MVFC.Veragi.Simulator.Domain.Handlers.Webhooks.ConfigureWebhookDestinations;

public sealed record ConfigureWebhookDestinationsCommand(WebhookDestinationsRequest Request) : IRequest<Result<WebhookDestinationsResponse>>;
