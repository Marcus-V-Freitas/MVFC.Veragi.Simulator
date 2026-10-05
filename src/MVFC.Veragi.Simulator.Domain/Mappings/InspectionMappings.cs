using MVFC.Veragi.Simulator.Shareable.Responses.Reconciliation;
using MVFC.Veragi.Simulator.Shareable.Responses.Webhooks;
using System.Text.Json.Nodes;
using MVFC.Veragi.Simulator.Domain.Entities;

namespace MVFC.Veragi.Simulator.Domain.Mappings;

public static class InspectionMappings
{
    public static WebhookDeliveryResponse ToResponse(this WebhookDeliveryEntity delivery) =>
        new(delivery.Id, delivery.OperationId, delivery.ExternalReference, delivery.MerchantCnpj, delivery.Kind, delivery.Attempts, delivery.Delivered, delivery.DeadLetter, delivery.NextAttemptAt, delivery.LastHttpStatus, JsonNode.Parse(delivery.Payload));

    public static WebhookReceiptResponse ToResponse(this WebhookReceiptEntity receipt) =>
        new(receipt.Id, receipt.Kind, receipt.MerchantCnpj, receipt.RequestId, receipt.ExternalReference, receipt.ReceivedAt, JsonNode.Parse(receipt.Payload));

    public static ReconciliationEntryResponse ToResponse(this ReconciliationEntity entry) =>
        new(entry.EntryId, entry.MerchantCnpj, JsonNode.Parse(entry.Payload), JsonNode.Parse(entry.AllocationsJson), entry.UnallocatedAmount);
}
