using MVFC.Veragi.Simulator.Domain.Services.Simulation;
using MediatR;
using OperationResult;

namespace MVFC.Veragi.Simulator.Domain.Handlers.Webhooks.ReplayDelivery;

public sealed class ReplayDeliveryHandler(ControlService control) : IRequestHandler<ReplayDeliveryCommand, Result<bool>>
{
    private readonly ControlService _control = control;

    public Task<Result<bool>> Handle(ReplayDeliveryCommand command, CancellationToken cancellationToken) =>
        _control.ReplayAsync(command.Id, cancellationToken);
}
