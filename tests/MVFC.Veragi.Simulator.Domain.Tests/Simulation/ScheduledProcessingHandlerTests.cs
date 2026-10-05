using MVFC.Veragi.Simulator.Domain.Services.Webhooks;
using FluentAssertions;
using NSubstitute;
using Xunit;
using MVFC.Veragi.Simulator.Domain.Handlers.Simulation.ProcessOperations;
using MVFC.Veragi.Simulator.Domain.Handlers.Webhooks.DispatchWebhooks;
using MVFC.Veragi.Simulator.Domain.Ports;
using MVFC.Veragi.Simulator.Shareable.Configuration;
using MVFC.Veragi.Simulator.Shareable.Enums;
using MVFC.Veragi.Simulator.Domain.Tests.Infrastructure;

namespace MVFC.Veragi.Simulator.Domain.Tests.Simulation;

public sealed class ScheduledProcessingHandlerTests
{
    [Fact]
    public async Task ProcessingCommandHonorsDueTimeAndForceOption()
    {
        // Arrange
        using var fixture = new ServiceFixture();
        await fixture.PrepareAsync();
        await fixture.ContractService.CreateAsync(fixture.Contract(), ServiceFixture.Key(), CancellationToken.None);
        var handler = new ProcessOperationsHandler(fixture.Processor);

        // Act
        var early = await handler.Handle(new ProcessOperationsCommand("contract", false), CancellationToken.None);
        var forced = await handler.Handle(new ProcessOperationsCommand("contract", true), CancellationToken.None);

        // Assert
        early.Value.Should().Be(0);
        forced.Value.Should().Be(1);
        fixture.Operations.Single(operation => operation.Kind == "contract").Status.Should().Be(ScheduleQueryStatusType.PROCESSED);
    }

    [Fact]
    public async Task DispatchCommandDeliversPersistedEventAndMarksItDelivered()
    {
        // Arrange
        using var fixture = new ServiceFixture();
        await fixture.PrepareAsync();
        var sender = Substitute.For<IWebhookSender>();

        sender.SendAsync(
                  Arg.Any<string>(),
                  Arg.Any<string>(),
                  Arg.Any<string>(),
                  Arg.Any<string>(),
                  Arg.Any<string>(),
                  CancellationToken.None)
              .Returns(OperationResult.Result.Success(204));

        var dispatcher = new WebhookDispatcher(fixture.Store, sender, fixture.DeliveryGate, fixture.Options, fixture.Clock, fixture.ScenarioService, fixture.RoutingService);
        var handler = new DispatchWebhooksHandler(dispatcher);

        // Act
        var result = await handler.Handle(new DispatchWebhooksCommand(), CancellationToken.None);

        // Assert
        result.Value.Should().Be(1);
        fixture.Deliveries.Should().ContainSingle(delivery => delivery.Delivered && delivery.Attempts == 1);
    }
}
