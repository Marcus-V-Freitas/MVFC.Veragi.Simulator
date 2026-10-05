namespace MVFC.Veragi.Simulator.Domain.Entities;

public sealed class WebhookDeliveryEntity
{
    public Guid Id { get; set; } = Guid.CreateVersion7(DateTimeOffset.UtcNow);

    public Guid OperationId { get; set; }

    public string Kind { get; set; } = "";

    public string Payload { get; set; } = "";

    public string PayloadKey { get; set; } = "";

    public string MerchantCnpj { get; set; } = "";

    public string ExternalReference { get; set; } = "";

    public int Attempts { get; set; }

    public bool Delivered { get; set; }

    public bool DeadLetter { get; set; }

    public DateTime NextAttemptAt { get; set; }

    public int? LastHttpStatus { get; set; }
}
