using System.Text.Json.Serialization;
using MVFC.Veragi.Simulator.Shareable.Enums;

namespace MVFC.Veragi.Simulator.Shareable.Responses.Schedules;

public sealed record Schedule(
    [property: JsonPropertyName("requestId")] string? RequestId = null,
    [property: JsonPropertyName("originType")] ScheduleQueryOriginType? OriginType = null,
    [property: JsonPropertyName("updatedAt")] string? UpdatedAt = null,
    [property: JsonPropertyName("merchantCnpj")] string? MerchantCnpj = null,
    [property: JsonPropertyName("acquirers")] List<SchedulePaymentAcquirer>? Acquirers = null
);
