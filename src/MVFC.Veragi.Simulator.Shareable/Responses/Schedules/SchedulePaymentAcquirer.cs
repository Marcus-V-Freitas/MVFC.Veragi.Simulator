using System.Text.Json.Serialization;

namespace MVFC.Veragi.Simulator.Shareable.Responses.Schedules;

public sealed record SchedulePaymentAcquirer(
    [property: JsonPropertyName("cnpj")] string? Cnpj = null,
    [property: JsonPropertyName("paymentArrangements")] List<SchedulePaymentArrangement>? PaymentArrangements = null
);
