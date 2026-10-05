using System.Text.Json.Serialization;
using MVFC.Veragi.Simulator.Shareable.Enums;

namespace MVFC.Veragi.Simulator.Shareable.Responses.Webhooks;

public sealed record ScheduleWebhookNotification(
    [property: JsonRequired, JsonPropertyName("status")] ScheduleQueryStatusType? Status = null,
    [property: JsonPropertyName("detalhe")] string? Detalhe = null,
    [property: JsonPropertyName("dadosConsultaAgenda")] ScheduleWebhookData? DadosConsultaAgenda = null
);
