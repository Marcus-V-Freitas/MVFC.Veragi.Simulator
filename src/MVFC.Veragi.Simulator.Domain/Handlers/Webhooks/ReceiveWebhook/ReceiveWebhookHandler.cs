using MVFC.Veragi.Simulator.Domain.Services.Simulation;
using MediatR;
using OperationResult;

namespace MVFC.Veragi.Simulator.Domain.Handlers.Webhooks.ReceiveWebhook;

public sealed class ReceiveWebhookHandler(ControlService control) : IRequestHandler<ReceiveWebhookCommand, Result<bool>>
{
    private readonly ControlService _control = control;

    public Task<Result<bool>> Handle(ReceiveWebhookCommand command, CancellationToken cancellationToken) =>
        _control.ReceiveAsync(command.Kind, command.Key, command.Payload, cancellationToken, command.ExternalReference);
}
