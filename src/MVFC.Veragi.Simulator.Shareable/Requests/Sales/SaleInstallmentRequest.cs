namespace MVFC.Veragi.Simulator.Shareable.Requests.Sales;

public sealed record SaleInstallmentRequest(
    decimal Amount = default,
    string? SettlementDate = null,
    int? SettlementBusinessDays = null
);
