using System.Text.Json.Serialization;

namespace MVFC.Veragi.Simulator.Shareable.Responses.Webhooks;

public sealed record ScheduleWebhookPaymentArrangement(
    [property: JsonPropertyName("codigo")] string? Codigo = null,
    [property: JsonPropertyName("unidadesRecebiveis")] List<ScheduleWebhookReceivableUnit>? UnidadesRecebiveis = null
);
