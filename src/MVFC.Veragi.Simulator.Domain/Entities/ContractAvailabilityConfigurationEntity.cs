using MVFC.Veragi.Simulator.Shareable.Enums;

namespace MVFC.Veragi.Simulator.Domain.Entities;

public sealed class ContractAvailabilityConfigurationEntity
{
    public Guid Id { get; set; } = Guid.CreateVersion7(DateTimeOffset.UtcNow);

    public ContractAvailabilityMode Mode { get; set; }
}
