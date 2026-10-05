using MVFC.Veragi.Simulator.Domain.Services.Webhooks;
using MediatR;
using OperationResult;

namespace MVFC.Veragi.Simulator.Domain.Handlers.Webhooks.DispatchWebhooks;

public sealed class DispatchWebhooksHandler(WebhookDispatcher dispatcher) : IRequestHandler<DispatchWebhooksCommand, Result<int>>
{
    private readonly WebhookDispatcher _dispatcher = dispatcher;

    public Task<Result<int>> Handle(DispatchWebhooksCommand command, CancellationToken cancellationToken) =>
        _dispatcher.DispatchAsync(cancellationToken);
}
