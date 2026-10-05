using System.Text.Json.Nodes;

namespace MVFC.Veragi.Simulator.Shareable.Responses.Webhooks;

public sealed record WebhookReceiptResponse(
    Guid Id,
    string Kind,
    string MerchantCnpj,
    string RequestId,
    string ExternalReference,
    DateTime ReceivedAt,
    JsonNode? Payload
);
