namespace MVFC.Veragi.Simulator.Shareable.Responses.Webhooks;

public sealed record ContractWebhookAcquirer(
    string Cnpj,
    IReadOnlyList<ContractWebhookArrangement> PaymentArrangements
);
