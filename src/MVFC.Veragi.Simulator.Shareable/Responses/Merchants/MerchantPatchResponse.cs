using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace MVFC.Veragi.Simulator.Shareable.Responses.Merchants;

public sealed record MerchantPatchResponse(
    [property: JsonPropertyName("message")] string? Message = null,
    [property: JsonPropertyName("timestamp")] string? Timestamp = null,
    [property: JsonPropertyName("traceId")] string? TraceId = null,
    [property: JsonPropertyName("data")] Merchant? Data = null,
    [property: JsonPropertyName("metadata")] JsonNode? Metadata = null
);
