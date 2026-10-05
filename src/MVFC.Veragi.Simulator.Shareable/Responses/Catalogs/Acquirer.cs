using System.Text.Json.Serialization;

namespace MVFC.Veragi.Simulator.Shareable.Responses.Catalogs;

public sealed record Acquirer(
    [property: JsonPropertyName("cnpj")] string? Cnpj = null,
    [property: JsonPropertyName("corporateName")] string? CorporateName = null
);
