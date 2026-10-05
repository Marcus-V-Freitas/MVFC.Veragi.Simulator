using MVFC.Veragi.Simulator.Shareable.Responses.Sales;
using MVFC.Veragi.Simulator.Domain.Entities;

namespace MVFC.Veragi.Simulator.Domain.Mappings;

public static class SaleMappings
{
    public static SimulatedSaleResponse ToResponse(this SimulatedSaleEntity sale) =>
        new(sale.Id, sale.BatchId, sale.MerchantCnpj, sale.AcquirerCnpj, sale.PaymentArrangementCode, sale.SettlementDate, sale.Amount, sale.SaleId ?? sale.Id, sale.InstallmentNumber ?? 1, sale.ExternalId);
}
