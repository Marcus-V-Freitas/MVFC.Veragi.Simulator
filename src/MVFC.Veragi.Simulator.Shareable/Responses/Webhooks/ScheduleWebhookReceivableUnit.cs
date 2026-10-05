using System.Text.Json.Serialization;

namespace MVFC.Veragi.Simulator.Shareable.Responses.Webhooks;

public sealed record ScheduleWebhookReceivableUnit(
    [property: JsonPropertyName("cnpjTitular")] string? CnpjTitular = null,
    [property: JsonPropertyName("dataVencimento")] string? DataVencimento = null,
    [property: JsonPropertyName("valorTotal")] decimal? ValorTotal = null,
    [property: JsonPropertyName("valorLivre")] decimal? ValorLivre = null
);
