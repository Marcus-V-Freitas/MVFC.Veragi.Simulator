using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace MVFC.Veragi.Simulator.Shareable.Responses.Common;

public sealed record ViolationItem(
    [property: JsonPropertyName("code")] string? Code = null,
    [property: JsonPropertyName("name")] string? Name = null,
    [property: JsonPropertyName("reason")] string? Reason = null,
    [property: JsonPropertyName("location")] string? Location = null,
    [property: JsonPropertyName("path")] string? Path = null,
    [property: JsonPropertyName("value")] JsonNode? Value = null
);
