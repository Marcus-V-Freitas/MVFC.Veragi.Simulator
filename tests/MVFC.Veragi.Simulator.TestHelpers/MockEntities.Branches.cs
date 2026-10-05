using Bogus;
using MVFC.Veragi.Simulator.Shareable.Requests.Sales;

namespace MVFC.Veragi.Simulator.TestHelpers;

public static partial class MockEntities
{
    public static SaleCreateRequest ExplicitSale() => new Faker<SaleCreateRequest>()
        .UseSeed(42)
        .CustomInstantiator(_ => new SaleCreateRequest(AcquirerCnpj, ArrangementCode, "sale-1", [new SaleInstallmentRequest(100, SettlementBusinessDays: 5)]))
        .Generate();
}
