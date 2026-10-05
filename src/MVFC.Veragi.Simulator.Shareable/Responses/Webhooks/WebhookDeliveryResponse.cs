using System.Text.Json.Nodes;

namespace MVFC.Veragi.Simulator.Shareable.Responses.Webhooks;

public sealed record WebhookDeliveryResponse(
    Guid EventKey,
    Guid OperationId,
    string ExternalReference,
    string MerchantCnpj,
    string Kind,
    int Attempts,
    bool Delivered,
    bool DeadLetter,
    DateTime NextAttemptAt,
    int? LastHttpStatus,
    JsonNode? Payload
);
