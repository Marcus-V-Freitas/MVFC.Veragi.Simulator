using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace MVFC.Veragi.Simulator.Shareable.Responses.Common;

public sealed record ConflictItem(
    [property: JsonPropertyName("code")] string? Code = null,
    [property: JsonPropertyName("title")] string? Title = null,
    [property: JsonPropertyName("detail")] string? Detail = null,
    [property: JsonPropertyName("location")] string? Location = null,
    [property: JsonPropertyName("path")] string? Path = null,
    [property: JsonPropertyName("value")] JsonNode? Value = null
);
