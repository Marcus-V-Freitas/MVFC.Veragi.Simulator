using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using OperationResult;
using MVFC.Veragi.Simulator.Api.Hosting;
using MVFC.Veragi.Simulator.Domain.Handlers.Simulation.ProcessOperations;
using MVFC.Veragi.Simulator.Domain.Handlers.Webhooks.DispatchWebhooks;
using MVFC.Veragi.Simulator.TestHelpers;
using Xunit;

namespace MVFC.Veragi.Simulator.Api.Tests.Hosting;

public sealed class SchedulerTests
{
    [Theory]
    [InlineData("schedule", "success")]
    [InlineData("contract", "success")]
    [InlineData("delivery", "success")]
    [InlineData("schedule", "exception")]
    [InlineData("schedule", "cancellation")]
    public async Task SchedulerDispatchesCycleAndStopsOrLogsFailure(string kind, string outcome)
    {
        // Arrange
        var options = MockEntities.Options() with 
        { 
            SchedulersEnabled = true, 
            PollIntervalSeconds = 1,
        };

        var observed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var scope = Substitute.For<IServiceScope>();
        var scopes = Substitute.For<IServiceScopeFactory>();
        var provider = Substitute.For<IServiceProvider>();
        var sender = Substitute.For<ISender>();

        scope.ServiceProvider
             .Returns(provider);

        scopes.CreateScope()
              .Returns(scope);

        provider.GetService(typeof(ISender))
                .Returns(sender);

        sender.Send(
                  Arg.Is<ProcessOperationsCommand>(command => command.Kind == kind && !command.Force),
                  Arg.Is<CancellationToken>(token => !token.IsCancellationRequested))
              .Returns(call => Process(call.ArgAt<CancellationToken>(1)));

        sender.Send(
                  Arg.Is<DispatchWebhooksCommand>(command => command != null),
                  Arg.Is<CancellationToken>(token => !token.IsCancellationRequested))
              .Returns(call => Process(call.ArgAt<CancellationToken>(1)));

        var logger = new RecordingLogger<SimulatorScheduler>();

        using var scheduler = new SimulatorScheduler(kind, scopes, options, logger);

        // Act
        await scheduler.StartAsync(CancellationToken.None);

        await observed.Task.WaitAsync(TimeSpan.FromSeconds(5));

        if (outcome == "exception")
        {
            var deadline = DateTime.UtcNow.AddSeconds(5);
            while (logger.Entries.Count == 0 && DateTime.UtcNow < deadline)
            {
                await Task.Delay(10);
            }
        }

        await scheduler.StopAsync(CancellationToken.None);

        await scheduler.ExecuteTask!;

        // Assert
        scopes.Received().CreateScope();
        scope.Received().Dispose();

        logger.Entries.Count(entry => entry.Level == LogLevel.Error)
              .Should().Be(outcome == "exception" ? 1 : 0);

        async Task<Result<int>> Process(CancellationToken cancellationToken)
        {
            observed.TrySetResult();

            if (outcome == "exception")
            {
                return await Task.FromException<Result<int>>(new InvalidOperationException("Database unavailable"));
            }

            if (outcome == "cancellation")
            {
                await Task.Delay(Timeout.Infinite, cancellationToken);
            }

            return Result.Success(0);
        }
    }
}
