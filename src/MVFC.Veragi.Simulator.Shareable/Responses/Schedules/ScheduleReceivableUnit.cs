using System.Text.Json.Serialization;

namespace MVFC.Veragi.Simulator.Shareable.Responses.Schedules;

public sealed record ScheduleReceivableUnit(
    [property: JsonPropertyName("settlementDate")] string? SettlementDate = null,
    [property: JsonPropertyName("totalAmount")] decimal? TotalAmount = null,
    [property: JsonPropertyName("freeAmount")] decimal? FreeAmount = null,
    [property: JsonPropertyName("holderCnpj")] string? HolderCnpj = null
);
