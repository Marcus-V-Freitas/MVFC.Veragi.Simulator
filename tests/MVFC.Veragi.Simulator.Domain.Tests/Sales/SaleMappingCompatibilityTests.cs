using FluentAssertions;
using MVFC.Veragi.Simulator.Domain.Mappings;
using MVFC.Veragi.Simulator.TestHelpers;
using Xunit;

namespace MVFC.Veragi.Simulator.Domain.Tests.Sales;

public sealed class SaleMappingCompatibilityTests
{
    [Fact]
    public void SaleWithGroupingMetadataPreservesSaleIdAndInstallmentNumber()
    {
        // Arrange
        var sale = MockEntities.Sale("2026-10-30", 150);
        sale.InstallmentNumber = 3;
        sale.ExternalId = "sale-3";

        // Act
        var response = sale.ToResponse();

        // Assert
        response.Id.Should().Be(sale.Id);
        response.SaleId.Should().Be(sale.SaleId!.Value);
        response.InstallmentNumber.Should().Be(3);
        response.ExternalId.Should().Be("sale-3");
        response.Amount.Should().Be(150);
    }

    [Fact]
    public void SaleWithoutGroupingMetadataUsesDocumentIdAndFirstInstallment()
    {
        // Arrange
        var sale = MockEntities.Sale("2026-10-30", 150);
        sale.SaleId = null;
        sale.InstallmentNumber = null;

        // Act
        var response = sale.ToResponse();

        // Assert
        response.Id.Should().Be(sale.Id);
        response.SaleId.Should().Be(sale.Id);
        response.InstallmentNumber.Should().Be(1);
        response.Amount.Should().Be(150);
        response.SettlementDate.Should().Be(sale.SettlementDate);
    }
}
