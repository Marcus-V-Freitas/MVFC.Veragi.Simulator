namespace MVFC.Veragi.Simulator.Domain.Entities;

public sealed class WebhookReceiptEntity
{
    public Guid Id { get; set; } = Guid.CreateVersion7(DateTimeOffset.UtcNow);

    public string Kind { get; set; } = "";

    public string EventKey { get; set; } = "";

    public string MerchantCnpj { get; set; } = "";

    public string RequestId { get; set; } = "";

    public string ExternalReference { get; set; } = "";

    public string Payload { get; set; } = "";

    public DateTime ReceivedAt { get; set; }
}
