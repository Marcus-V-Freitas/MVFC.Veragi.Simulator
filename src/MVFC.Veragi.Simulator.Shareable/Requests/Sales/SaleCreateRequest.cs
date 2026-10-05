namespace MVFC.Veragi.Simulator.Shareable.Requests.Sales;

public sealed record SaleCreateRequest(
    string AcquirerCnpj = "",
    string PaymentArrangementCode = "",
    string? ExternalId = null,
    List<SaleInstallmentRequest>? Installments = null
);
