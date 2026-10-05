namespace MVFC.Veragi.Simulator.Domain.Entities;

public sealed class SalesBatchEntity
{
    public Guid Id { get; set; } = Guid.CreateVersion7(DateTimeOffset.UtcNow);

    public string BatchKey { get; set; } = "";

    public string MerchantCnpj { get; set; } = "";

    public string RequestHash { get; set; } = "";

    public string ResponseJson { get; set; } = "";
}
