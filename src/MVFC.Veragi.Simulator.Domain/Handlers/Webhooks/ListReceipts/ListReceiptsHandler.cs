using MVFC.Veragi.Simulator.Domain.Services.Simulation;
using MVFC.Veragi.Simulator.Shareable.Responses.Webhooks;
using MediatR;

namespace MVFC.Veragi.Simulator.Domain.Handlers.Webhooks.ListReceipts;

public sealed class ListReceiptsHandler(InspectionService inspection) : IRequestHandler<ListReceiptsCommand, IReadOnlyList<WebhookReceiptResponse>>
{
    private readonly InspectionService _inspection = inspection;

    public Task<IReadOnlyList<WebhookReceiptResponse>> Handle(ListReceiptsCommand command, CancellationToken cancellationToken) =>
        _inspection.ListReceiptsAsync(command.Kind, command.MerchantCnpj, command.RequestId, cancellationToken);
}
