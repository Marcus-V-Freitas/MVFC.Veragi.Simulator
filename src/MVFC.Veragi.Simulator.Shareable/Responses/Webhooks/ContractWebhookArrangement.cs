namespace MVFC.Veragi.Simulator.Shareable.Responses.Webhooks;

public sealed record ContractWebhookArrangement(
    string Code,
    IReadOnlyList<ContractWebhookUnit> ReceivableUnits
);
