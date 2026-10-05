namespace MVFC.Veragi.Simulator.Shareable.Responses.Reconciliation;

public sealed record ExternalAnticipationResponse(
    Guid Id,
    string MerchantCnpj,
    string AcquirerCnpj,
    string PaymentArrangementCode,
    string SettlementDate,
    decimal Amount
);
