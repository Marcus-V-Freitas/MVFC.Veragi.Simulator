using MVFC.Veragi.Simulator.Shareable.Enums;

namespace MVFC.Veragi.Simulator.Shareable.Configuration;

public sealed record SimulatorOptions(
    ContractAvailabilityMode ContractAvailabilityMode,
    ScheduleWebhookSchema ScheduleWebhookSchema,
    string TimeZone,
    int ProcessingDelaySeconds,
    int PollIntervalSeconds,
    int MinimumBusinessDays,
    List<string> Holidays,
    int MaxDeliveryAttempts,
    int RetryBaseSeconds,
    int WebhookTimeoutSeconds,
    bool EnableControlEndpoints,
    bool SchedulersEnabled
);