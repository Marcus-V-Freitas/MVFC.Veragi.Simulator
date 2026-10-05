using System.Text.Json.Nodes;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using OperationResult;
using MVFC.Veragi.Simulator.Api.Extensions;
using MVFC.Veragi.Simulator.Shareable.Results;
using Xunit;

namespace MVFC.Veragi.Simulator.Api.Tests.Mapping;

public sealed class ResultHttpMappingTests
{
    private static readonly string TestTraceId = Guid.NewGuid().ToString();

    [Theory]
    [InlineData(400, "Validation Error")]
    [InlineData(401, "Unauthorized")]
    [InlineData(403, "Forbidden")]
    [InlineData(404, "Not Found")]
    [InlineData(409, "Conflict")]
    [InlineData(422, "Unprocessable Entity")]
    [InlineData(500, "Internal Server Error")]
    [InlineData(503, "Internal Server Error")]
    public async Task ExpectedFailurePreservesStatusCodeAndProblemMetadata(int status, string title)
    {
        // Arrange
        await using var services = new ServiceCollection().AddLogging().AddOptions().BuildServiceProvider();
        var context = Context(services);
        var failure = new SimulationFailureException(status, "SIMULATED_FAILURE", "Configured failure");
        Result<int> result = failure;

        // Act
        await result.ToHttp(context).ExecuteAsync(context);
        var problem = Body(context);

        // Assert
        context.Response.StatusCode.Should().Be(status);
        context.Response.ContentType.Should().StartWith("application/problem+json");
        problem["title"]!.GetValue<string>().Should().Be(title);
        problem["detail"]!.GetValue<string>().Should().Be(failure.Message);
        problem["errorCode"]!.GetValue<string>().Should().Be("SIMULATED_FAILURE");
        problem["traceId"]!.GetValue<string>().Should().Be(TestTraceId);
        Guid.TryParse(problem["traceId"]!.GetValue<string>(), out _).Should().BeTrue();
    }

    [Theory]
    [InlineData(200, false)]
    [InlineData(201, false)]
    [InlineData(202, false)]
    [InlineData(204, true)]
    public async Task SuccessfulResultHonorsResponseStatusAndBodyPolicy(int status, bool noBody)
    {
        // Arrange
        await using var services = new ServiceCollection().AddLogging().AddOptions().BuildServiceProvider();
        var context = Context(services);
        var result = Result.Success(42);

        // Act
        await result.ToHttp(context, status, noBody).ExecuteAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(status);

        if (noBody)
            context.Response.Body.Length.Should().Be(0);
        else
        {
            var envelope = Body(context);
            envelope["data"]!.GetValue<int>().Should().Be(42);
            envelope["traceId"]!.GetValue<string>().Should().Be(TestTraceId);
            Guid.TryParse(envelope["traceId"]!.GetValue<string>(), out _).Should().BeTrue();
        }
    }

    [Fact]
    public async Task UnexpectedFailureHidesInfrastructureMessageAndReturnsInternalError()
    {
        // Arrange
        await using var services = new ServiceCollection().AddLogging().AddOptions().BuildServiceProvider();
        var context = Context(services);
        var failure = new InvalidOperationException("Database connection details");

        // Act
        await ResultHttpExtensions.Error(failure, context).ExecuteAsync(context);
        var problem = Body(context);

        // Assert
        context.Response.StatusCode.Should().Be(500);
        problem["title"]!.GetValue<string>().Should().Be("Internal Server Error");
        problem["detail"]!.GetValue<string>().Should().Be("Unexpected internal error");
        problem["errorCode"]!.GetValue<string>().Should().Be("INTERNAL_ERROR");
        problem["traceId"]!.GetValue<string>().Should().Be(TestTraceId);
        Guid.TryParse(problem["traceId"]!.GetValue<string>(), out _).Should().BeTrue();
        problem["violations"].Should().BeNull();
    }

    [Fact]
    public async Task NonGuidTraceIdentifierIsStandardizedToGuid()
    {
        // Arrange
        await using var services = new ServiceCollection().AddLogging().AddOptions().BuildServiceProvider();
        var context = Context(services);
        context.TraceIdentifier = "legacy-aspnet-0HN123:0001";
        var result = Result.Success(100);

        // Act
        await result.ToHttp(context).ExecuteAsync(context);
        var envelope = Body(context);

        // Assert
        var traceId = envelope["traceId"]!.GetValue<string>();
        traceId.Should().NotBe("legacy-aspnet-0HN123:0001");
        Guid.TryParse(traceId, out _).Should().BeTrue();
        context.TraceIdentifier.Should().Be(traceId);
    }

    private static DefaultHttpContext Context(IServiceProvider services)
    {
        var context = new DefaultHttpContext { RequestServices = services, TraceIdentifier = TestTraceId };
        context.Request.Path = "/test";
        context.Response.Body = new MemoryStream();

        return context;
    }

    private static JsonNode Body(DefaultHttpContext context)
    {
        context.Response.Body.Position = 0;

        return JsonNode.Parse(context.Response.Body)!;
    }
}
