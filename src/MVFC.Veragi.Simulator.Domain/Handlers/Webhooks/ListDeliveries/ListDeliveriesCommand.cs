using MVFC.Veragi.Simulator.Shareable.Responses.Webhooks;
using MediatR;
using OperationResult;

namespace MVFC.Veragi.Simulator.Domain.Handlers.Webhooks.ListDeliveries;

public sealed record ListDeliveriesCommand(
    string? Kind,
    string? MerchantCnpj
) : IRequest<IReadOnlyList<WebhookDeliveryResponse>>;
