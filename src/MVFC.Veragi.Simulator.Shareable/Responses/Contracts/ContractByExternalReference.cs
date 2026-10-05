using System.Text.Json.Serialization;
using MVFC.Veragi.Simulator.Shareable.Enums;

namespace MVFC.Veragi.Simulator.Shareable.Responses.Contracts;

public sealed record ContractByExternalReference(
    [property: JsonPropertyName("financierContractId")] string? FinancierContractId = null,
    [property: JsonPropertyName("contractorCnpj")] string? ContractorCnpj = null,
    [property: JsonPropertyName("status")] ContractStatusType? Status = null,
    [property: JsonPropertyName("effectType")] EffectType? EffectType = null,
    [property: JsonPropertyName("signatureDate")] string? SignatureDate = null,
    [property: JsonPropertyName("debtBalanceAmount")] decimal? DebtBalanceAmount = null,
    [property: JsonPropertyName("guaranteedLimitAmount")] decimal? GuaranteedLimitAmount = null,
    [property: JsonPropertyName("minimumBalanceAmount")] decimal? MinimumBalanceAmount = null,
    [property: JsonPropertyName("reachedGuarantees")] List<ContractByExternalReferenceGuarantee>? ReachedGuarantees = null
);
