namespace MVFC.Veragi.Simulator.Domain.Entities;

public sealed class MerchantEntity
{
    public Guid Id { get; set; } = Guid.CreateVersion7(DateTimeOffset.UtcNow);

    public string Cnpj { get; set; } = "";

    public string Payload { get; set; } = "";

    public bool IsDeleted { get; set; }
}
