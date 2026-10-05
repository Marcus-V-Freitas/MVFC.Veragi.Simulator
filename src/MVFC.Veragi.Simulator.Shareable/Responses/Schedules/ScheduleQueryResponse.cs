using System.Text.Json.Serialization;

namespace MVFC.Veragi.Simulator.Shareable.Responses.Schedules;

public sealed record ScheduleQueryResponse([property: JsonPropertyName("requestId")] string? RequestId = null);
