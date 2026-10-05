using MVFC.Veragi.Simulator.Domain.Services.Simulation;
using MVFC.Veragi.Simulator.Shareable.Responses.Webhooks;
using MediatR;

namespace MVFC.Veragi.Simulator.Domain.Handlers.Webhooks.ListDeliveries;

public sealed class ListDeliveriesHandler(InspectionService inspection) : IRequestHandler<ListDeliveriesCommand, IReadOnlyList<WebhookDeliveryResponse>>
{
    private readonly InspectionService _inspection = inspection;

    public Task<IReadOnlyList<WebhookDeliveryResponse>> Handle(ListDeliveriesCommand command, CancellationToken cancellationToken) =>
        _inspection.ListDeliveriesAsync(command.Kind, command.MerchantCnpj, cancellationToken);
}
