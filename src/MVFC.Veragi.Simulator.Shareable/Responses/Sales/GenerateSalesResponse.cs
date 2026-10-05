namespace MVFC.Veragi.Simulator.Shareable.Responses.Sales;

public sealed record GenerateSalesResponse(
    Guid BatchId,
    string MerchantCnpj,
    int SalesCount,
    decimal TotalAmount,
    string FirstSettlementDate,
    string LastSettlementDate,
    int InstallmentCount = 0
);
