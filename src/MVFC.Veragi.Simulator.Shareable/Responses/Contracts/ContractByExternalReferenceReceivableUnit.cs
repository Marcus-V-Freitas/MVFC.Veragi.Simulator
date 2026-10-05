using System.Text.Json.Serialization;
using MVFC.Veragi.Simulator.Shareable.Enums;

namespace MVFC.Veragi.Simulator.Shareable.Responses.Contracts;

public sealed record ContractByExternalReferenceReceivableUnit(
    [property: JsonPropertyName("settlementDate")] string? SettlementDate = null,
    [property: JsonPropertyName("divisionRule")] DivisionRuleType? DivisionRule = null,
    [property: JsonPropertyName("percentage")] decimal? Percentage = null,
    [property: JsonPropertyName("requestedAmount")] decimal? RequestedAmount = null,
    [property: JsonPropertyName("reachedAmount")] decimal? ReachedAmount = null
);
