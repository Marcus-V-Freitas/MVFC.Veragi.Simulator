namespace MVFC.Veragi.Simulator.Shareable.Requests.Sales;

public sealed record GenerateSalesRequest(
    List<SaleCreateRequest>? Sales = null,
    int Days = 10,
    int SalesPerDay = 5,
    decimal AmountPerSale = 2000m,
    string? StartSettlementDate = null,
    List<string>? AcquirerCnpjs = null,
    List<string>? ArrangementCodes = null
);
