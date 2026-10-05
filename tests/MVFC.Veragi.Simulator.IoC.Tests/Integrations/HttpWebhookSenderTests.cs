using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using MVFC.Veragi.Simulator.IoC.Integrations;
using MVFC.Veragi.Simulator.Shareable.Results;
using MVFC.Veragi.Simulator.TestHelpers;
using Xunit;

namespace MVFC.Veragi.Simulator.IoC.Tests.Integrations;

public sealed class HttpWebhookSenderTests
{
    private static string Key() => Guid.CreateVersion7(DateTimeOffset.UtcNow).ToString();

    [Theory]
    [InlineData("schedule", HttpStatusCode.NoContent)]
    [InlineData("contract", HttpStatusCode.OK)]
    [InlineData("contract", HttpStatusCode.ServiceUnavailable)]
    public async Task SenderPreservesPayloadHeadersAndReturnsPartnerStatus(string kind, HttpStatusCode status)
    {
        // Arrange
        var destinationUrl = "https://example.com/webhook";
        var eventKey = Key();
        var send = Substitute.For<Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>>();

        send(
            Arg.Is<HttpRequestMessage>(x => x.Method == HttpMethod.Post && x.RequestUri!.AbsoluteUri == destinationUrl),
            Arg.Is<CancellationToken>(x => !x.IsCancellationRequested))
            .Returns(async call =>
            {
                var request = call.Arg<HttpRequestMessage>();
                request.Headers.Authorization.Should().BeNull();
                request.Headers.GetValues("Idempotency-Key").Should().Equal(eventKey);
                request.Headers.GetValues("X-Simulator-Event-Type").Should().Equal(kind);
                request.Headers.Contains("X-Contract-External-Reference").Should().Be(kind == "contract");
                (await request.Content!.ReadAsStringAsync()).Should().Be("{\"status\":\"PROCESSED\"}");

                return new HttpResponseMessage(status);
            });

        using var client = new HttpClient(new TestHttpMessageHandler(send));
        var logger = new RecordingLogger<HttpWebhookSender>();
        var sender = new HttpWebhookSender(client, logger);

        // Act
        var result = await sender.SendAsync(kind, eventKey, "{\"status\":\"PROCESSED\"}", "SIM-test", destinationUrl, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().Be((int)status < 300);

        if (result.IsSuccess)
            result.Value.Should().Be((int)status);
        else
            ((SimulationFailureException)result.Exception!).StatusCode.Should().Be((int)status);
    }

    [Theory]
    [InlineData("", "WEBHOOK_NOT_CONFIGURED")]
    [InlineData("relative", "WEBHOOK_NOT_CONFIGURED")]
    [InlineData("ftp://example.com", "WEBHOOK_NOT_CONFIGURED")]
    public async Task MissingOrUnsupportedDestinationFailsBeforeHttp(string url, string code)
    {
        // Arrange
        var send = Substitute.For<Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>>();
        using var client = new HttpClient(new TestHttpMessageHandler(send));
        var sender = new HttpWebhookSender(client, Substitute.For<ILogger<HttpWebhookSender>>());

        // Act
        var result = await sender.SendAsync("schedule", Key(), "{}", "", url, CancellationToken.None);

        // Assert
        ((SimulationFailureException)result.Exception!).Code.Should().Be(code);
        await send.DidNotReceive().Invoke(Arg.Any<HttpRequestMessage>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(false, "WEBHOOK_NETWORK_FAILURE")]
    [InlineData(true, "WEBHOOK_TIMEOUT")]
    public async Task InfrastructureFailureBecomesResultAndDoesNotExposeExceptionMessage(
        bool timeout,
        string code
    )
    {
        // Arrange
        using var client = new HttpClient(new TestHttpMessageHandler((_, _) => timeout ? Task.FromCanceled<HttpResponseMessage>(new CancellationToken(true)) : Task.FromException<HttpResponseMessage>(new HttpRequestException("secret"))));
        var logger = new RecordingLogger<HttpWebhookSender>();
        var sender = new HttpWebhookSender(client, logger);

        // Act
        var result = await sender.SendAsync("schedule", Key(), "{}", "", "https://example.com/webhook", CancellationToken.None);

        // Assert
        ((SimulationFailureException)result.Exception!).Code.Should().Be(code);
        result.Exception.Message.Should().NotContain("secret");
    }

    [Fact]
    public async Task CallerCancellationIsPropagated()
    {
        // Arrange
        using var client = new HttpClient(new TestHttpMessageHandler((_, ct) => Task.FromCanceled<HttpResponseMessage>(ct)));
        var sender = new HttpWebhookSender(client, Substitute.For<ILogger<HttpWebhookSender>>());
        var ct = new CancellationToken(true);

        // Act
        var act = () => sender.SendAsync("schedule", Key(), "{}", "", "https://example.com/webhook", ct);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }
}
