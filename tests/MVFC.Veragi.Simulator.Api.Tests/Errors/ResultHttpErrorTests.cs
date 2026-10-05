using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using MVFC.Veragi.Simulator.Api.Extensions;
using MVFC.Veragi.Simulator.Shareable.Results;
using Xunit;

namespace MVFC.Veragi.Simulator.Api.Tests.Errors;

public sealed class ResultHttpErrorTests
{
    [Theory]
    [InlineData(401)]
    [InlineData(403)]
    public async Task ErrorPreservesExplicitFailureStatusCode(int status)
    {
        // Arrange
        await using var services = new ServiceCollection().AddLogging().AddOptions().BuildServiceProvider();
        var context = new DefaultHttpContext
        {
            RequestServices = services
        };
        context.Response.Body = new MemoryStream();
        var failure = new SimulationFailureException(status, "SIMULATED_FAILURE", "Configured failure");

        // Act
        await ResultHttpExtensions.Error(failure, context).ExecuteAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(status);
    }
}
