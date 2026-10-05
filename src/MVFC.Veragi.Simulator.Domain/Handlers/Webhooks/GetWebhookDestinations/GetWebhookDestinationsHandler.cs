using MVFC.Veragi.Simulator.Domain.Services.Webhooks;
using MVFC.Veragi.Simulator.Shareable.Responses.Webhooks;
using MediatR;
using OperationResult;

namespace MVFC.Veragi.Simulator.Domain.Handlers.Webhooks.GetWebhookDestinations;

public sealed class GetWebhookDestinationsHandler(WebhookRoutingService service) : IRequestHandler<GetWebhookDestinationsCommand, Result<WebhookDestinationsResponse>>
{
    private readonly WebhookRoutingService _service = service;

    public Task<Result<WebhookDestinationsResponse>> Handle(GetWebhookDestinationsCommand request, CancellationToken cancellationToken) =>
        _service.GetAsync(cancellationToken);
}
