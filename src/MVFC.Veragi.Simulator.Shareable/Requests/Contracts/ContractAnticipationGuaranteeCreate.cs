using System.Text.Json.Serialization;

namespace MVFC.Veragi.Simulator.Shareable.Requests.Contracts;

public sealed record ContractAnticipationGuaranteeCreate(
    [property: JsonPropertyName("receivableUnitHolderCnpj")] string? ReceivableUnitHolderCnpj = null,
    [property: JsonPropertyName("finalUserReceiverCnpj")] string? FinalUserReceiverCnpj = null,
    [property: JsonPropertyName("acquirerCnpj")] string? AcquirerCnpj = null,
    [property: JsonPropertyName("paymentArrangementCode")] string? PaymentArrangementCode = null,
    [property: JsonPropertyName("settlementDate")] string? SettlementDate = null,
    [property: JsonPropertyName("definedAmount")] decimal? DefinedAmount = null
);
