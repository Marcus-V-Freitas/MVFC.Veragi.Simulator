using System.Text.Json.Serialization;

namespace MVFC.Veragi.Simulator.Shareable.Responses.Webhooks;

public sealed record ScheduleWebhookPaymentAcquirer(
    [property: JsonPropertyName("cnpj")] string? Cnpj = null,
    [property: JsonPropertyName("arranjosPagamento")] List<ScheduleWebhookPaymentArrangement>? ArranjosPagamento = null
);
