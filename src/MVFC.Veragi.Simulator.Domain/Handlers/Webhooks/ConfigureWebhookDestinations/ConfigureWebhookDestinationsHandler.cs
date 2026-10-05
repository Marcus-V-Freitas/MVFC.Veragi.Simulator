using MVFC.Veragi.Simulator.Domain.Services.Webhooks;
using MVFC.Veragi.Simulator.Shareable.Responses.Webhooks;
using MediatR;
using OperationResult;

namespace MVFC.Veragi.Simulator.Domain.Handlers.Webhooks.ConfigureWebhookDestinations;

public sealed class ConfigureWebhookDestinationsHandler(WebhookRoutingService service) : IRequestHandler<ConfigureWebhookDestinationsCommand, Result<WebhookDestinationsResponse>>
{
    private readonly WebhookRoutingService _service = service;

    public Task<Result<WebhookDestinationsResponse>> Handle(ConfigureWebhookDestinationsCommand request, CancellationToken cancellationToken) => 
        _service.ConfigureAsync(request.Request, cancellationToken);
}
