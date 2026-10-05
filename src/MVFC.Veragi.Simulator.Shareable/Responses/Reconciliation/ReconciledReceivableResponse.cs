namespace MVFC.Veragi.Simulator.Shareable.Responses.Reconciliation;

public sealed record ReconciledReceivableResponse(
    string ExternalReference,
    string HolderCnpj,
    string AcquirerCnpj,
    string PaymentArrangementCode,
    string SettlementDate,
    decimal Amount
);
