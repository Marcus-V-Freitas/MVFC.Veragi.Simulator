using System.Text.Json.Serialization;
using MVFC.Veragi.Simulator.Shareable.Enums;

namespace MVFC.Veragi.Simulator.Shareable.Responses.Contracts;

public sealed record ContractByContractorAndSituation(
    [property: JsonPropertyName("externalReference")] string? ExternalReference = null,
    [property: JsonPropertyName("financierContractId")] string? FinancierContractId = null,
    [property: JsonPropertyName("status")] ContractStatusType? Status = null,
    [property: JsonPropertyName("contractorCnpj")] string? ContractorCnpj = null,
    [property: JsonPropertyName("effectType")] EffectType? EffectType = null,
    [property: JsonPropertyName("signatureDate")] string? SignatureDate = null,
    [property: JsonPropertyName("dueDate")] string? DueDate = null,
    [property: JsonPropertyName("guaranteedLimitAmount")] decimal? GuaranteedLimitAmount = null,
    [property: JsonPropertyName("reachedAmount")] decimal? ReachedAmount = null,
    [property: JsonPropertyName("updatedAmount")] decimal? UpdatedAmount = null
);
