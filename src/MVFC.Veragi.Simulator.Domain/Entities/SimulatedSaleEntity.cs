namespace MVFC.Veragi.Simulator.Domain.Entities;

public sealed class SimulatedSaleEntity
{
    public Guid Id { get; set; } = Guid.CreateVersion7(DateTimeOffset.UtcNow);

    public Guid? SaleId { get; set; }

    public int? InstallmentNumber { get; set; }

    public string? ExternalId { get; set; }

    public Guid BatchId { get; set; }

    public string MerchantCnpj { get; set; } = "";

    public string AcquirerCnpj { get; set; } = "";

    public string PaymentArrangementCode { get; set; } = "";

    public string SettlementDate { get; set; } = "";

    public decimal Amount { get; set; }

    public DateTime CreatedAt { get; set; }
}
