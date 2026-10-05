using System.Text.Json.Serialization;

namespace MVFC.Veragi.Simulator.Shareable.Responses.Catalogs;

public sealed record PaymentArrangement(
    [property: JsonPropertyName("code")] string? Code = null,
    [property: JsonPropertyName("description")] string? Description = null
);
