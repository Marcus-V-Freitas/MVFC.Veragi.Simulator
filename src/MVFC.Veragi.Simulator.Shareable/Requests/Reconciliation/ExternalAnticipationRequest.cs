namespace MVFC.Veragi.Simulator.Shareable.Requests.Reconciliation;

public sealed record ExternalAnticipationRequest(
    string? AcquirerCnpj = null,
    string? PaymentArrangementCode = null,
    string? SettlementDate = null,
    decimal Amount = default
);
