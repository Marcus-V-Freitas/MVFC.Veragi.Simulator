using System.Text.Json.Serialization;

namespace MVFC.Veragi.Simulator.Shareable.Responses.Contracts;

public sealed record ContractByExternalReferenceAcquirer(
    [property: JsonPropertyName("cnpj")] string? Cnpj = null,
    [property: JsonPropertyName("paymentArrangements")] List<ContractByExternalReferencePaymentArrangement>? PaymentArrangements = null
);
