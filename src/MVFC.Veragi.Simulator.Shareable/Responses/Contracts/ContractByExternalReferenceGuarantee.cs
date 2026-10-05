using System.Text.Json.Serialization;

namespace MVFC.Veragi.Simulator.Shareable.Responses.Contracts;

public sealed record ContractByExternalReferenceGuarantee(
    [property: JsonPropertyName("receivableUnitHolderCnpj")] string? ReceivableUnitHolderCnpj = null,
    [property: JsonPropertyName("acquirers")] List<ContractByExternalReferenceAcquirer>? Acquirers = null
);
