using System.Text.Json.Serialization;
using MVFC.Veragi.Simulator.Shareable.Enums;

namespace MVFC.Veragi.Simulator.Shareable.Requests.Schedules;

public sealed record ScheduleQueryRequest(
    [property: JsonPropertyName("merchantCnpj")] string? MerchantCnpj = null,
    [property: JsonPropertyName("queryType")] ScheduleQueryType? QueryType = null
);
