namespace MVFC.Veragi.Simulator.Domain.Entities;

public sealed class ReconciliationEntity
{
    public Guid Id { get; set; } = Guid.CreateVersion7(DateTimeOffset.UtcNow);

    public Guid EntryId { get; set; }

    public string MerchantCnpj { get; set; } = "";

    public string AllocationsJson { get; set; } = "[]";

    public decimal UnallocatedAmount { get; set; }

    public string Payload { get; set; } = "";
}
