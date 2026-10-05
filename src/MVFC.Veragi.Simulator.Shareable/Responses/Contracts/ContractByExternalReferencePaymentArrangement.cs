using System.Text.Json.Serialization;

namespace MVFC.Veragi.Simulator.Shareable.Responses.Contracts;

public sealed record ContractByExternalReferencePaymentArrangement(
    [property: JsonPropertyName("code")] string? Code = null,
    [property: JsonPropertyName("receivableUnits")] List<ContractByExternalReferenceReceivableUnit>? ReceivableUnits = null
);
