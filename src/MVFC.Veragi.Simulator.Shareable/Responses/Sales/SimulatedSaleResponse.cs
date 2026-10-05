namespace MVFC.Veragi.Simulator.Shareable.Responses.Sales;

public sealed record SimulatedSaleResponse(
    Guid Id,
    Guid BatchId,
    string MerchantCnpj,
    string AcquirerCnpj,
    string PaymentArrangementCode,
    string SettlementDate,
    decimal Amount,
    Guid SaleId,
    int InstallmentNumber,
    string? ExternalId
);
