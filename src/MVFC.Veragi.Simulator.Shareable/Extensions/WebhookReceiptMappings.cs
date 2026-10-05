using MVFC.Veragi.Simulator.Shareable.Requests.Webhooks;
using MVFC.Veragi.Simulator.Shareable.Responses.Webhooks;
using System.Text.Json.Nodes;

namespace MVFC.Veragi.Simulator.Shareable.Extensions;

public static class WebhookReceiptMappings
{
    public static ReceivedWebhookRequest ToReceivedWebhook(
        this ScheduleWebhookNotification request,
        string kind,
        string key,
        string? reference,
        string traceId
    ) => new(kind, key, reference, request.DadosConsultaAgenda?.CnpjEstabelecimento, request.DadosConsultaAgenda?.IdRequisicao, request.Status, JsonNode.Parse(request.ToJson())!, traceId);

    public static ReceivedWebhookRequest ToReceivedWebhook(
        this ContractWebhookNotification request,
        string kind,
        string key,
        string? reference,
        string traceId
    ) => new(kind, key, reference, request.ScheduleQueryData?.MerchantCnpj, request.ScheduleQueryData?.RequestId, request.Status, JsonNode.Parse(request.ToJson())!, traceId);
}
