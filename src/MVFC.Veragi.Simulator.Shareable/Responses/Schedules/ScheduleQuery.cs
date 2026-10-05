using System.Text.Json.Serialization;
using MVFC.Veragi.Simulator.Shareable.Enums;

namespace MVFC.Veragi.Simulator.Shareable.Responses.Schedules;

public sealed record ScheduleQuery(
    [property: JsonPropertyName("status")] ScheduleQueryStatusType? Status = null,
    [property: JsonPropertyName("detail")] string? Detail = null,
    [property: JsonPropertyName("scheduleQueryData")] Schedule? ScheduleQueryData = null
);
