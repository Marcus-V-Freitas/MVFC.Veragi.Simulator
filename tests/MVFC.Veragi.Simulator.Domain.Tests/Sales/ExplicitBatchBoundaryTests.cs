using FluentAssertions;
using NSubstitute;
using MVFC.Veragi.Simulator.Domain.Entities;
using MVFC.Veragi.Simulator.Shareable.Requests.Sales;
using MVFC.Veragi.Simulator.TestHelpers;
using Xunit;
using MVFC.Veragi.Simulator.Domain.Tests.Infrastructure;

namespace MVFC.Veragi.Simulator.Domain.Tests.Sales;

public sealed class ExplicitBatchBoundaryTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(5001)]
    public async Task BatchOutsideInstallmentCountLimitIsRejectedAtomically(int count)
    {
        // Arrange
        using var fixture = new ServiceFixture();
        fixture.Merchants.Add(MockEntities.MerchantEntity());
        var installments = Enumerable.Repeat(new SaleInstallmentRequest(100, SettlementBusinessDays: 5), count).ToList();
        var sale = MockEntities.ExplicitSale() with { Installments = installments };
        var request = new GenerateSalesRequest(Sales: [sale]);

        // Act
        var result = await fixture.SalesService.GenerateAsync(MockEntities.MerchantCnpj, request, ServiceFixture.Key(), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        fixture.Store.DidNotReceive().AddSale(Arg.Any<SimulatedSaleEntity>());
        fixture.Store.DidNotReceive().AddSalesBatch(Arg.Any<SalesBatchEntity>());
        await fixture.Store.DidNotReceive().SaveAsync(CancellationToken.None);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("sale-1")]
    public async Task ExplicitSaleWorksWithOrWithoutOptionalExternalReference(string? externalId)
    {
        // Arrange
        using var fixture = new ServiceFixture();
        fixture.Merchants.Add(MockEntities.MerchantEntity());
        var sale = MockEntities.ExplicitSale() with { ExternalId = externalId };
        var request = new GenerateSalesRequest(Sales: [sale]);

        // Act
        var result = await fixture.SalesService.GenerateAsync(MockEntities.MerchantCnpj, request, ServiceFixture.Key(), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalAmount.Should().Be(100);
        fixture.Sales.Should().ContainSingle().Which.ExternalId.Should().Be(externalId);
    }
}
