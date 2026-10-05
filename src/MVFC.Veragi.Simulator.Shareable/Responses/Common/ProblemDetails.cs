using System.Text.Json.Serialization;

namespace MVFC.Veragi.Simulator.Shareable.Responses.Common;

public sealed record ProblemDetails(
    [property: JsonPropertyName("type")] string? Type = null,
    [property: JsonPropertyName("title")] string? Title = null,
    [property: JsonPropertyName("status")] int? Status = null,
    [property: JsonPropertyName("detail")] string? Detail = null,
    [property: JsonPropertyName("instance")] string? Instance = null,
    [property: JsonPropertyName("timestamp")] string? Timestamp = null,
    [property: JsonPropertyName("traceId")] string? TraceId = null,
    [property: JsonPropertyName("errorCode")] string? ErrorCode = null,
    [property: JsonPropertyName("violations")] List<ViolationItem>? Violations = null,
    [property: JsonPropertyName("conflicts")] List<ConflictItem>? Conflicts = null
);
