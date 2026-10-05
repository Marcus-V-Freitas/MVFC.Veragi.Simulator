namespace MVFC.Veragi.Simulator.Shareable.Requests.Webhooks;

public sealed record WebhookDestinationsRequest(string? ScheduleUrl = null, string? ContractUrl = null);
