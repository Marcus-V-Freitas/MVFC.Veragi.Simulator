using MVFC.Veragi.Simulator.Domain.Services.Schedules;
using FluentAssertions;
using System.Globalization;
using MVFC.Veragi.Simulator.Domain.Mappings;
using MVFC.Veragi.Simulator.Shareable.Enums;
using MVFC.Veragi.Simulator.TestHelpers;
using Xunit;
using MVFC.Veragi.Simulator.Domain.Tests.Infrastructure;

namespace MVFC.Veragi.Simulator.Domain.Tests.Schedules;

public sealed class AgendaWindowTests
{
    [Theory]
    [InlineData(QueryWindowType.P6M, 6)]
    [InlineData(QueryWindowType.P1Y, 12)]
    [InlineData(QueryWindowType.P2Y, 24)]
    public void AgendaIncludesBothWindowBoundariesAndExcludesReceivablesOutsideConfiguredScope(QueryWindowType window, int months)
    {
        // Arrange
        using var fixture = new ServiceFixture();
        var merchant = MockEntities.Merchant();
        merchant = merchant with
        {
            ReceivablesScheduleConfig = merchant.ReceivablesScheduleConfig! with
            {
                QueryWindow = window
            }
        };
        var today = fixture.Calendar.Today;
        var horizon = today.AddMonths(months);
        var inScope = new[]
        {
            MockEntities.Sale(today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
            MockEntities.Sale(horizon.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))
        };
        var wrongMerchant = MockEntities.Sale(today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        wrongMerchant.MerchantCnpj = "33185894000174";
        var wrongAcquirer = MockEntities.Sale(today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        wrongAcquirer.AcquirerCnpj = "01027058000191";
        var wrongArrangement = MockEntities.Sale(today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        wrongArrangement.PaymentArrangementCode = "MCC";
        var sales = inScope.Concat([MockEntities.Sale(today.AddDays(-1).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)), MockEntities.Sale(horizon.AddDays(1).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)), wrongMerchant, wrongAcquirer, wrongArrangement]).ToArray();
        var operation = MockEntities.Operation("schedule", MockEntities.MerchantCnpj, fixture.Clock.GetUtcNow().UtcDateTime);
        var generator = new ScheduleGenerator(fixture.Calendar);
        var schedule = generator.Generate(operation, merchant, [], sales, fixture.Clock.GetUtcNow().UtcDateTime);

        // Act
        var units = schedule.Acquirers!.Single().PaymentArrangements!.Single().ReceivableUnits!;

        // Assert
        units.Select(x => x.SettlementDate).Should().Equal(today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), horizon.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        units.Sum(x => x.TotalAmount).Should().Be(200);
        units.Should().OnlyContain(x => x.FreeAmount == x.TotalAmount);
    }

    [Fact]
    public void ErrorAgendaHasNoReceivableHierarchyEvenWhenSalesExist()
    {
        // Arrange
        using var fixture = new ServiceFixture();
        var operation = MockEntities.Operation("schedule", MockEntities.MerchantCnpj, fixture.Clock.GetUtcNow().UtcDateTime);
        operation.Outcome = ScheduleQueryStatusType.ERROR;
        var generator = new ScheduleGenerator(fixture.Calendar);

        // Act
        var schedule = generator.Generate(operation, MockEntities.Merchant(), [], [MockEntities.Sale(fixture.Calendar.Today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))], fixture.Clock.GetUtcNow().UtcDateTime);

        // Assert
        schedule.Acquirers.Should().BeNull();
        schedule.RequestId.Should().Be(operation.Id.ToString());
    }

    [Fact]
    public void LegacySaleWithoutGroupingFieldsUsesItsStorageIdAndFirstInstallment()
    {
        // Arrange
        var sale = MockEntities.Sale("2026-10-30", 42);
        sale.SaleId = null;
        sale.InstallmentNumber = null;

        // Act
        var response = sale.ToResponse();

        // Assert
        response.SaleId.Should().Be(sale.Id);
        response.InstallmentNumber.Should().Be(1);
        response.Amount.Should().Be(42);
    }
}
