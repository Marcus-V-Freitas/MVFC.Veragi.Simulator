using MediatR;

namespace MVFC.Veragi.Simulator.Domain.Handlers.Webhooks.FailWebhook;

public sealed class FailWebhookHandler : IRequestHandler<FailWebhookCommand, int>
{
    public Task<int> Handle(FailWebhookCommand command, CancellationToken cancellationToken) => 
        Task.FromResult(503);
}
