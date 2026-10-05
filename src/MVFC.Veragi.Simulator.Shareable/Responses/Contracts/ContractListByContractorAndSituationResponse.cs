using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace MVFC.Veragi.Simulator.Shareable.Responses.Contracts;

public sealed record ContractListByContractorAndSituationResponse(
    [property: JsonPropertyName("message")] string? Message = null,
    [property: JsonPropertyName("timestamp")] string? Timestamp = null,
    [property: JsonPropertyName("traceId")] string? TraceId = null,
    [property: JsonPropertyName("data")] List<ContractByContractorAndSituation>? Data = null,
    [property: JsonPropertyName("metadata")] JsonNode? Metadata = null
);
