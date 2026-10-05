using System.Text.Json.Nodes;

namespace MVFC.Veragi.Simulator.Shareable.Responses.Webhooks;

public sealed record WebhookEventResponse(
    string EventKey,
    string Kind,
    string MerchantCnpj,
    string RequestId,
    string? ExternalReference,
    JsonNode Payload,
    DateTimeOffset ReceivedAt,
    string ProcessingStatus,
    string TraceId
);
