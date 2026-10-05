namespace MVFC.Veragi.Simulator.Domain.Entities;

public sealed class ExternalAnticipationEntity
{
    public Guid Id { get; set; } = Guid.CreateVersion7(DateTimeOffset.UtcNow);

    public string MerchantCnpj { get; set; } = "";

    public string AcquirerCnpj { get; set; } = "";

    public string PaymentArrangementCode { get; set; } = "";

    public string SettlementDate { get; set; } = "";

    public decimal Amount { get; set; }

    public string IdempotencyKey { get; set; } = "";

    public string RequestHash { get; set; } = "";
}
