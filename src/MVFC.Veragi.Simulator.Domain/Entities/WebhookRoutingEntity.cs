namespace MVFC.Veragi.Simulator.Domain.Entities;

public sealed class WebhookRoutingEntity
{
    public Guid Id { get; set; } = Guid.CreateVersion7(DateTimeOffset.UtcNow);

    public string ScheduleUrl { get; set; } = "";

    public string ContractUrl { get; set; } = "";
}
