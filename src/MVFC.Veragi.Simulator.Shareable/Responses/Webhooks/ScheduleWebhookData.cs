using System.Text.Json.Serialization;
using MVFC.Veragi.Simulator.Shareable.Enums;

namespace MVFC.Veragi.Simulator.Shareable.Responses.Webhooks;

public sealed record ScheduleWebhookData(
    [property: JsonPropertyName("idRequisicao")] string? IdRequisicao = null,
    [property: JsonPropertyName("tipoOrigem")] ScheduleQueryOriginType? TipoOrigem = null,
    [property: JsonPropertyName("dataHoraAtualizacao")] string? DataHoraAtualizacao = null,
    [property: JsonPropertyName("cnpjEstabelecimento")] string? CnpjEstabelecimento = null,
    [property: JsonPropertyName("credenciadoras")][property: JsonIgnore(Condition = JsonIgnoreCondition.Never)] List<ScheduleWebhookPaymentAcquirer>? Credenciadoras = null
);
