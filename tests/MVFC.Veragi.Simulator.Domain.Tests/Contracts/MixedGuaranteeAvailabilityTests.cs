using System.Globalization;
using FluentAssertions;
using Xunit;
using MVFC.Veragi.Simulator.Shareable.Enums;
using MVFC.Veragi.Simulator.Shareable.Extensions;
using MVFC.Veragi.Simulator.Shareable.Responses.Contracts;
using MVFC.Veragi.Simulator.TestHelpers;
using MVFC.Veragi.Simulator.Domain.Tests.Infrastructure;

namespace MVFC.Veragi.Simulator.Domain.Tests.Contracts;

public sealed class MixedGuaranteeAvailabilityTests
{
    [Fact]
    public async Task ExhaustedGuaranteeAndAvailableGuaranteeProduceOnlyPositiveReachedBalance()
    {
        // Arrange
        using var fixture = new ServiceFixture();
        var scheduleId = await fixture.PrepareAsync();
        await fixture.ContractService.CreateAsync(fixture.Contract(1000), ServiceFixture.Key(), CancellationToken.None);
        await fixture.Processor.ProcessAsync("contract", true, CancellationToken.None);
        var date = fixture.Calendar.AddBusinessDays(fixture.Calendar.Today, fixture.Options.MinimumBusinessDays + 1).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        fixture.Sales.Add(MockEntities.Sale(date, 600));
        await fixture.Processor.RepublishScheduleAsync(scheduleId, CancellationToken.None);
        var request = fixture.Contract(900);
        request = request with
        {
            Guarantees = [request.Guarantees![0] with { DefinedAmount = 300 }, request.Guarantees[0] with { SettlementDate = date, DefinedAmount = 600 }]
        };

        // Act
        var result = await fixture.ContractService.CreateAsync(request, ServiceFixture.Key(), CancellationToken.None);
        await fixture.Processor.ProcessAsync("contract", true, CancellationToken.None);
        var details = fixture.Operations.Last(operation => operation.Kind == "contract").ResultJson.FromJson<ContractByExternalReference>()!;

        // Assert
        result.IsSuccess.Should().BeTrue();
        details.Status.Should().Be(ContractStatusType.Active);
        var units = details.ReachedGuarantees![0].Acquirers![0].PaymentArrangements![0].ReceivableUnits!;
        units.Should().HaveCount(2);
        units[0].ReachedAmount.Should().Be(0);
        units[1].ReachedAmount.Should().Be(600);
    }
}
