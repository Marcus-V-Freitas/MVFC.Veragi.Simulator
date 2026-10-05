using FluentAssertions;
using MVFC.Veragi.Simulator.Domain.Entities;
using MVFC.Veragi.Simulator.Domain.Tests.Infrastructure;
using MVFC.Veragi.Simulator.Shareable.Enums;
using MVFC.Veragi.Simulator.Shareable.Extensions;
using MVFC.Veragi.Simulator.Shareable.Requests.Simulation;
using MVFC.Veragi.Simulator.Shareable.Responses.Contracts;
using MVFC.Veragi.Simulator.Shareable.Responses.Schedules;
using MVFC.Veragi.Simulator.Shareable.Results;
using MVFC.Veragi.Simulator.TestHelpers;
using Xunit;

namespace MVFC.Veragi.Simulator.Domain.Tests.Contracts;

public sealed class ContractAvailabilityTests
{
    [Fact]
    public async Task ExhaustedUnitIsRejectedBeforeOperationAndWebhookAreCreated()
    {
        // Arrange
        using var fixture = new ServiceFixture();
        await fixture.PrepareAsync();
        await fixture.ContractService.CreateAsync(fixture.Contract(1000), ServiceFixture.Key(), CancellationToken.None);
        await fixture.Processor.ProcessAsync("contract", true, CancellationToken.None);
        var operations = fixture.Operations.Count;
        var deliveries = fixture.Deliveries.Count;

        // Act
        var result = await fixture.ContractService.CreateAsync(fixture.Contract(), ServiceFixture.Key(), CancellationToken.None);

        // Assert
        ((SimulationFailureException)result.Exception!).Code.Should().Be("INSUFFICIENT_RECEIVABLE_BALANCE");
        fixture.Operations.Should().HaveCount(operations);
        fixture.Deliveries.Should().HaveCount(deliveries);
    }

    [Fact]
    public async Task PartialAnticipationCanConsumeRemainderThenRejectFurtherContracts()
    {
        // Arrange
        using var fixture = new ServiceFixture();
        await fixture.PrepareAsync();
        await fixture.ContractService.CreateAsync(fixture.Contract(250), ServiceFixture.Key(), CancellationToken.None);
        await fixture.Processor.ProcessAsync("contract", true, CancellationToken.None);

        // Act
        var remainder = await fixture.ContractService.CreateAsync(fixture.Contract(750), ServiceFixture.Key(), CancellationToken.None);
        await fixture.Processor.ProcessAsync("contract", true, CancellationToken.None);
        var exhausted = await fixture.ContractService.CreateAsync(fixture.Contract(1), ServiceFixture.Key(), CancellationToken.None);

        // Assert
        remainder.IsSuccess.Should().BeTrue();
        exhausted.IsSuccess.Should().BeFalse();
        var schedule = fixture.Operations.Single(operation => operation.Kind == "schedule").ResultJson.FromJson<ScheduleQuery>()!;
        schedule.ScheduleQueryData!.Acquirers![0].PaymentArrangements![0].ReceivableUnits![0].FreeAmount.Should().Be(0);
    }

    [Fact]
    public async Task PositiveBalanceBelowRequestedAmountStillProducesPartialSuccess()
    {
        // Arrange
        using var fixture = new ServiceFixture();
        await fixture.PrepareAsync();

        // Act
        var result = await fixture.ContractService.CreateAsync(fixture.Contract(1200), ServiceFixture.Key(), CancellationToken.None);
        await fixture.Processor.ProcessAsync("contract", true, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var details = fixture.Operations.Single(operation => operation.Kind == "contract").ResultJson.FromJson<ContractByExternalReference>()!;
        details.Status.Should().Be(ContractStatusType.Active);
        details.ReachedGuarantees![0].Acquirers![0].PaymentArrangements![0].ReceivableUnits![0].ReachedAmount.Should().Be(1000);
    }

    [Fact]
    public async Task PendingReservationBlocksAnotherImmediateAdmission()
    {
        // Arrange
        using var fixture = new ServiceFixture();
        await fixture.PrepareAsync();
        await fixture.ContractService.CreateAsync(fixture.Contract(1000), ServiceFixture.Key(), CancellationToken.None);

        // Act
        var result = await fixture.ContractService.CreateAsync(fixture.Contract(1), ServiceFixture.Key(), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        fixture.Operations.Count(operation => operation.Kind == "contract").Should().Be(1);
    }

    [Fact]
    public async Task CancelledContractReleasesItsReservation()
    {
        // Arrange
        using var fixture = new ServiceFixture();
        await fixture.PrepareAsync();
        await fixture.ContractService.CreateAsync(fixture.Contract(1000), ServiceFixture.Key(), CancellationToken.None);
        await fixture.ScenarioService.ConfigureAsync("contract-process", new SimulationScenarioRequest(ContractStatuses: [ContractStatusType.Cancelled]), CancellationToken.None);
        await fixture.Processor.ProcessAsync("contract", true, CancellationToken.None);

        // Act
        var result = await fixture.ContractService.CreateAsync(fixture.Contract(1000), ServiceFixture.Key(), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task DeferredModeAcceptsExhaustedBalanceThenEmitsErrorWithoutUpdatingAgenda()
    {
        // Arrange
        using var fixture = new ServiceFixture();
        await fixture.PrepareAsync();
        await fixture.ContractService.CreateAsync(fixture.Contract(1000), ServiceFixture.Key(), CancellationToken.None);
        await fixture.Processor.ProcessAsync("contract", true, CancellationToken.None);
        fixture.Options = fixture.Options with { ContractAvailabilityMode = ContractAvailabilityMode.DEFERRED };
        var agendaEvents = fixture.Deliveries.Count(delivery => delivery.Kind == "schedule");

        // Act
        var accepted = await fixture.ContractService.CreateAsync(fixture.Contract(1), ServiceFixture.Key(), CancellationToken.None);
        await fixture.Processor.ProcessAsync("contract", true, CancellationToken.None);

        // Assert
        accepted.IsSuccess.Should().BeTrue();
        var operation = fixture.Operations.Last(item => item.Kind == "contract");
        operation.Status.Should().Be(ScheduleQueryStatusType.ERROR);
        operation.ResultJson.FromJson<ContractByExternalReference>()!.Status.Should().Be(ContractStatusType.Cancelled);
        fixture.Deliveries.Count(delivery => delivery.Kind == "schedule").Should().Be(agendaEvents);
        fixture.Deliveries.Last(delivery => delivery.Kind == "contract").Payload.Should().Contain("ERROR");
    }

    [Fact]
    public async Task ProcessingRechecksBalanceEvenWhenAdmissionWasImmediate()
    {
        // Arrange
        using var fixture = new ServiceFixture();
        await fixture.PrepareAsync();
        await fixture.ContractService.CreateAsync(fixture.Contract(1000), ServiceFixture.Key(), CancellationToken.None);
        var guarantee = fixture.Contract().Guarantees![0];
        fixture.ExternalAnticipations.Add(new ExternalAnticipationEntity { MerchantCnpj = MockEntities.MerchantCnpj, AcquirerCnpj = guarantee.AcquirerCnpj!, PaymentArrangementCode = guarantee.PaymentArrangementCode!, SettlementDate = guarantee.SettlementDate!, Amount = 1000 });

        // Act
        await fixture.Processor.ProcessAsync("contract", true, CancellationToken.None);

        // Assert
        fixture.Operations.Single(operation => operation.Kind == "contract").Status.Should().Be(ScheduleQueryStatusType.ERROR);
        fixture.Deliveries.Count(delivery => delivery.Kind == "schedule").Should().Be(2);
        var agenda = fixture.Operations.Single(operation => operation.Kind == "schedule").ResultJson.FromJson<ScheduleQuery>()!;
        agenda.ScheduleQueryData!.Acquirers![0].PaymentArrangements![0].ReceivableUnits![0].FreeAmount.Should().Be(0);
    }

    [Fact]
    public async Task IdempotentRetryReturnsOriginalContractAfterBalanceIsConsumed()
    {
        // Arrange
        using var fixture = new ServiceFixture();
        await fixture.PrepareAsync();
        var request = fixture.Contract(1000);
        var key = ServiceFixture.Key();
        var original = await fixture.ContractService.CreateAsync(request, key, CancellationToken.None);
        await fixture.Processor.ProcessAsync("contract", true, CancellationToken.None);

        // Act
        var repeated = await fixture.ContractService.CreateAsync(request, key, CancellationToken.None);

        // Assert
        repeated.IsSuccess.Should().BeTrue();
        repeated.Value![0].ExternalReference.Should().Be(original.Value![0].ExternalReference);
        fixture.Operations.Count(operation => operation.Kind == "contract").Should().Be(1);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SelectedUnitsDetermineAvailabilityInsteadOfEntireMerchantAgenda(bool selectAvailableUnit)
    {
        // Arrange
        using var fixture = new ServiceFixture();
        var scheduleId = await fixture.PrepareAsync();
        var merchant = fixture.Merchants.Single().Payload.FromJson<Shareable.Responses.Merchants.Merchant>()!;
        fixture.Merchants.Single().Payload = (merchant with
        {
            ReceivablesScheduleConfig = merchant.ReceivablesScheduleConfig! with
            {
                ArrangementCodes = ["VCC", "MCC"]
            }
        }).ToJson();
        var extraSale = MockEntities.Sale(fixture.Contract().Guarantees![0].SettlementDate!, 500);
        extraSale.PaymentArrangementCode = "MCC";
        fixture.Sales.Add(extraSale);
        await fixture.Processor.RepublishScheduleAsync(scheduleId, CancellationToken.None);
        await fixture.ContractService.CreateAsync(fixture.Contract(1000), ServiceFixture.Key(), CancellationToken.None);
        await fixture.Processor.ProcessAsync("contract", true, CancellationToken.None);
        var request = fixture.Contract(100);

        if (selectAvailableUnit)
            request = request with
            {
                Guarantees = [request.Guarantees![0] with
                {
                    PaymentArrangementCode = "MCC"
                }]
            };

        // Act
        var result = await fixture.ContractService.CreateAsync(request, ServiceFixture.Key(), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().Be(selectAvailableUnit);
    }
}
