using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using MVFC.Veragi.Simulator.Api.Errors;
using MVFC.Veragi.Simulator.TestHelpers;
using Xunit;

namespace MVFC.Veragi.Simulator.Api.Tests.Errors;

public sealed class ExceptionHandlerTests
{
    [Theory]
    [InlineData(true, 400)]
    [InlineData(false, 500)]
    public async Task HandlerReturnsProblemAndLogsOnlyUnexpectedFailures(bool bindingFailure, int status)
    {
        // Arrange
        var services = new ServiceCollection().AddLogging().AddOptions().BuildServiceProvider();
        var context = new DefaultHttpContext
        {
            RequestServices = services
        };
        context.Response.Body = new MemoryStream();
        var logger = new RecordingLogger<SimulatorExceptionHandler>();
        var handler = new SimulatorExceptionHandler(logger);
        Exception exception = bindingFailure ? new BadHttpRequestException("Invalid request") : new InvalidOperationException("Database unavailable");

        // Act
        var handled = await handler.TryHandleAsync(context, exception, CancellationToken.None);

        // Assert
        handled.Should().BeTrue();
        context.Response.StatusCode.Should().Be(status);
        logger.Entries.Should().HaveCount(bindingFailure ? 0 : 1);
        Guid.TryParse(context.TraceIdentifier, out _).Should().BeTrue();
        await services.DisposeAsync();
    }

    [Fact]
    public async Task HandlerDoesNotWriteToStartedResponse()
    {
        // Arrange
        var context = new DefaultHttpContext();

        var response = Substitute.For<IHttpResponseFeature>();

        response.HasStarted
                .Returns(true);

        context.Features.Set(response);

        var logger = new RecordingLogger<SimulatorExceptionHandler>();
        var handler = new SimulatorExceptionHandler(logger);

        // Act
        var handled = await handler.TryHandleAsync(context, new InvalidOperationException(), CancellationToken.None);

        // Assert
        handled.Should().BeFalse();
        logger.Entries.Should().BeEmpty();
    }

    [Fact]
    public async Task HandlerIgnoresCallerCancellation()
    {
        // Arrange
        var cancellationToken = new CancellationToken(true);
        var context = new DefaultHttpContext
        {
            RequestAborted = cancellationToken
        };
        var logger = new RecordingLogger<SimulatorExceptionHandler>();
        var handler = new SimulatorExceptionHandler(logger);

        // Act
        var handled = await handler.TryHandleAsync(context, new OperationCanceledException(cancellationToken), cancellationToken);

        // Assert
        handled.Should().BeTrue();
        logger.Entries.Should().BeEmpty();
    }
}
