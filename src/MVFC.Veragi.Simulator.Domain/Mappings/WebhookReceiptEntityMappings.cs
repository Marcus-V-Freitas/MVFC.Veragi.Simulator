using System.Text.Json.Nodes;
using MVFC.Veragi.Simulator.Domain.Entities;
using MVFC.Veragi.Simulator.Shareable.Extensions;
using MVFC.Veragi.Simulator.Shareable.Responses.Webhooks;

namespace MVFC.Veragi.Simulator.Domain.Mappings;

public static class WebhookReceiptEntityMappings
{
    public static WebhookReceiptEntity ToReceipt(
        this string payload,
        string kind,
        string eventKey,
        string externalReference,
        DateTime receivedAt
    )
    {
        var body = payload.FromJson<JsonObject>()!;
        var notification = body.ContainsKey("dadosConsultaAgenda") ? payload.FromJson<ScheduleWebhookNotification>()!.ToReceivedWebhook(kind, eventKey, externalReference, "") : payload.FromJson<ContractWebhookNotification>()!.ToReceivedWebhook(kind, eventKey, externalReference, "");

        return new WebhookReceiptEntity
        {
            EventKey = eventKey,
            Kind = kind,
            Payload = payload,
            MerchantCnpj = notification.MerchantCnpj ?? "",
            RequestId = notification.RequestId ?? "",
            ExternalReference = externalReference,
            ReceivedAt = receivedAt,
        };
    }
}
