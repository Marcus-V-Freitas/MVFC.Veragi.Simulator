namespace MVFC.Veragi.Simulator.Shareable.Responses.Webhooks;

public sealed record WebhookDestinationsResponse(string ScheduleUrl, string ContractUrl, bool Persisted);
