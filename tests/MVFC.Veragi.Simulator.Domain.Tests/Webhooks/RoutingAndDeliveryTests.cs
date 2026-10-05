using MVFC.Veragi.Simulator.Domain.Services.Contracts;
using MVFC.Veragi.Simulator.Domain.Services.Receivables;
using MVFC.Veragi.Simulator.Domain.Services.Webhooks;
using MVFC.Veragi.Simulator.Shareable.Requests.Webhooks;
using FluentAssertions;
using NSubstitute;
using MVFC.Veragi.Simulator.Domain.Entities;
using MVFC.Veragi.Simulator.Domain.Ports;
using MVFC.Veragi.Simulator.Shareable.Configuration;
using MVFC.Veragi.Simulator.Shareable.Enums;
using MVFC.Veragi.Simulator.Shareable.Extensions;
using MVFC.Veragi.Simulator.TestHelpers;
using Xunit;
using MVFC.Veragi.Simulator.Domain.Tests.Infrastructure;

namespace MVFC.Veragi.Simulator.Domain.Tests.Webhooks;

public sealed class RoutingAndDeliveryTests
{
    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("invalid\nheader")]
    [InlineData("invalid\rheader")]
    public async Task DestinationsRejectEmptyAndInvalidUrls(string url)
    {
        // Arrange
        using var fixture = new ServiceFixture();
        var request = new WebhookDestinationsRequest(url, "https://example.com/contract");

        // Act
        var result = await fixture.RoutingService.ConfigureAsync(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        fixture.Routing.Should().BeEmpty();
    }

    [Fact]
    public async Task SuccessfulDeliveryMarksEventAndPreservesItsIdentity()
    {
        // Arrange
        using var fixture = new ServiceFixture();
        await fixture.PrepareAsync();
        await fixture.RoutingService.ConfigureAsync(new WebhookDestinationsRequest("https://example.com/schedule", "https://example.com/contract"), CancellationToken.None);
        var delivery = fixture.Deliveries.Single();
        var sender = Substitute.For<IWebhookSender>();

        sender.SendAsync(
                  delivery.Kind,
                  delivery.Id.ToString(),
                  delivery.Payload,
                  delivery.ExternalReference,
                  "https://example.com/schedule",
                  CancellationToken.None)
              .Returns(OperationResult.Result.Success(204));

        var dispatcher = new WebhookDispatcher(fixture.Store, sender, fixture.DeliveryGate, fixture.Options, fixture.Clock, fixture.ScenarioService, fixture.RoutingService);

        // Act
        var result = await dispatcher.DispatchAsync(CancellationToken.None);

        // Assert
        result.Value.Should().Be(1);
        delivery.Delivered.Should().BeTrue();
        delivery.DeadLetter.Should().BeFalse();
        delivery.Attempts.Should().Be(1);
        delivery.LastHttpStatus.Should().Be(204);
    }

    [Fact]
    public async Task ContractWithoutPreviouslyProcessedScheduleDoesNotTriggerAgendaUpdateAfterRegistration()
    {
        // Arrange
        using var fixture = new ServiceFixture();
        await fixture.PrepareAsync();
        await fixture.ContractService.CreateAsync(fixture.Contract(), ServiceFixture.Key(), CancellationToken.None);
        fixture.Operations.RemoveAll(x => x.Kind == "schedule");

        // Act
        var result = await fixture.Processor.ProcessAsync("contract", true, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        fixture.Deliveries.Count(x => x.Kind == "schedule").Should().Be(1);
    }

    [Fact]
    public void ProcessingReservationIsIgnoredWhenCalculatingRegisteredOnlyCommitments()
    {
        // Arrange
        using var fixture = new ServiceFixture();
        var pending = new ScheduledOperationEntity
        {
            Status = ScheduleQueryStatusType.PROCESSING,
            RequestJson = fixture.Contract().ToJson()
        };

        // Act
        var commitments = ReceivableBalances.Commitments([pending], false);

        // Assert
        commitments.Should().BeEmpty();
    }

    [Theory]
    [InlineData("cnpj-list")]
    [InlineData("situation")]
    [InlineData("cnpj-get")]
    [InlineData("reference-short")]
    [InlineData("reference-long")]
    public async Task ContractQueriesRejectInvalidFilters(string failure)
    {
        // Arrange
        using var fixture = new ServiceFixture();

        // Act
        var result = failure is "cnpj-list" or "situation" ? (await fixture.ContractService.ListAsync(failure == "cnpj-list" ? "invalid" : MockEntities.MerchantCnpj, failure == "situation" ? "UNKNOWN" : null, CancellationToken.None)).IsSuccess : (await fixture.ContractService.GetAsync(failure == "cnpj-get" ? "invalid" : MockEntities.MerchantCnpj, failure == "reference-long" ? new string('a', 46) : "", CancellationToken.None)).IsSuccess;

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ContractRejectsPrecisionInTotalOrIndividualGuarantees(bool total)
    {
        // Arrange
        using var fixture = new ServiceFixture();
        var contract = fixture.Contract(100.001m);

        if (!total)
            contract = contract with
            {
                RequestedAmount = 200.01m,
                Guarantees = [contract.Guarantees![0] with
                {
                    DefinedAmount = 100.005m
                }, contract.Guarantees[0] with
                {
                    DefinedAmount = 100.005m
                }

                ]
            };

        var rules = new ContractRules(fixture.Calendar, fixture.Options);

        // Act
        var result = rules.Validate(contract);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Exception!.Message.Should().Contain("two decimal places");
    }
}
