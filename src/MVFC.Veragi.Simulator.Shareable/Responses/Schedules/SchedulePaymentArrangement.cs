using System.Text.Json.Serialization;

namespace MVFC.Veragi.Simulator.Shareable.Responses.Schedules;

public sealed record SchedulePaymentArrangement(
    [property: JsonPropertyName("code")] string? Code = null,
    [property: JsonPropertyName("receivableUnits")] List<ScheduleReceivableUnit>? ReceivableUnits = null
);
