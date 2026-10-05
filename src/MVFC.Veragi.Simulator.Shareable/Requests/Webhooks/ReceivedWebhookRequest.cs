using System.Text.Json.Nodes;
using MVFC.Veragi.Simulator.Shareable.Enums;

namespace MVFC.Veragi.Simulator.Shareable.Requests.Webhooks;

public sealed record ReceivedWebhookRequest(
    string Kind,
    string EventKey,
    string? ExternalReference,
    string? MerchantCnpj,
    string? RequestId,
    ScheduleQueryStatusType? BusinessStatus,
    JsonNode Payload,
    string TraceId
);
