using MVFC.Veragi.Simulator.Shareable.Responses.Webhooks;
using MediatR;
using OperationResult;

namespace MVFC.Veragi.Simulator.Domain.Handlers.Webhooks.ListReceipts;

public sealed record ListReceiptsCommand(
    string? Kind,
    string? MerchantCnpj,
    string? RequestId
) : IRequest<IReadOnlyList<WebhookReceiptResponse>>;
