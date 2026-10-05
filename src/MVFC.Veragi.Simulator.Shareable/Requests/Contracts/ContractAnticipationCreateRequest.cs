using MVFC.Veragi.Simulator.Shareable.Responses.Contracts;
using System.Text.Json.Serialization;

namespace MVFC.Veragi.Simulator.Shareable.Requests.Contracts;

public sealed record ContractAnticipationCreateRequest(
    [property: JsonPropertyName("financierContractId")] string? FinancierContractId = null,
    [property: JsonPropertyName("contractorCnpj")] string? ContractorCnpj = null,
    [property: JsonPropertyName("wallet")] string? Wallet = null,
    [property: JsonPropertyName("signatureDate")] string? SignatureDate = null,
    [property: JsonPropertyName("requestedAmount")] decimal? RequestedAmount = null,
    [property: JsonPropertyName("guarantees")] List<ContractAnticipationGuaranteeCreate>? Guarantees = null,
    [property: JsonPropertyName("settlementBankAccount")] SettlementAccountContract? SettlementBankAccount = null
);
