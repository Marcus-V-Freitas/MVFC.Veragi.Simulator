using MVFC.Veragi.Simulator.Shareable.Enums;

namespace MVFC.Veragi.Simulator.Domain.Entities;

public sealed class ScheduledOperationEntity
{
    public Guid Id { get; set; } = Guid.CreateVersion7(DateTimeOffset.UtcNow);

    public string Kind { get; set; } = "";

    public string MerchantCnpj { get; set; } = "";

    public string IdempotencyKey { get; set; } = "";

    public string RequestHash { get; set; } = "";

    public string RequestJson { get; set; } = "";

    public string ResultJson { get; set; } = "";

    public string SettlementBankAccountCode { get; set; } = "";

    public string ExternalReference { get; set; } = "";

    public ScheduleQueryStatusType Outcome { get; set; } = ScheduleQueryStatusType.PROCESSED;

    public ScheduleQueryStatusType Status { get; set; } = ScheduleQueryStatusType.PROCESSING;

    public bool? ProgressNotified { get; set; }

    public DateTime DueAt { get; set; }

    public DateTime CreatedAt { get; set; }
}
