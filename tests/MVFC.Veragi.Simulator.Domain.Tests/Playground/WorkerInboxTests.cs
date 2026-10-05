using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using MVFC.Veragi.Simulator.Shareable.Enums;
using MVFC.Veragi.Simulator.Shareable.Responses.Webhooks;
using MVFC.Veragi.Simulator.TestHelpers;
using Xunit;

namespace MVFC.Veragi.Simulator.Domain.Tests.Playground;

public sealed class WorkerInboxTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task HealthEndpointReturnsOk()
    {
        // Act
        using var response = await _client.GetAsync("/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task PostPortugueseWebhookReturnsOk()
    {
        // Arrange
        var key = Guid.CreateVersion7(DateTimeOffset.UtcNow).ToString();
        var requestId = Guid.CreateVersion7(DateTimeOffset.UtcNow).ToString();
        var payload = new ScheduleWebhookNotification(
            ScheduleQueryStatusType.PROCESSED,
            "Done",
            new ScheduleWebhookData(requestId, ScheduleQueryOriginType.ONLINE, DateTimeOffset.UtcNow.ToString("O"), MockEntities.MerchantCnpj, [])
        );

        using var request = new HttpRequestMessage(HttpMethod.Post, "/webhooks/card-receivables/schedules/updated")
        {
            Content = JsonContent.Create(payload)
        };
        request.Headers.Add("Idempotency-Key", key);
        request.Headers.Add("X-Simulator-Event-Type", "schedule");

        // Act
        using var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task PostEnglishWebhookReturnsOk()
    {
        // Arrange
        var key = Guid.CreateVersion7(DateTimeOffset.UtcNow).ToString();
        var requestId = Guid.CreateVersion7(DateTimeOffset.UtcNow).ToString();
        var payload = new ContractWebhookNotification(
            ScheduleQueryStatusType.PROCESSED,
            "Done",
            new ContractWebhookData(requestId, ScheduleQueryOriginType.ONLINE, DateTimeOffset.UtcNow.ToString("O"), MockEntities.MerchantCnpj, [])
        );

        using var request = new HttpRequestMessage(HttpMethod.Post, "/webhooks/card-receivable/schedules")
        {
            Content = JsonContent.Create(payload)
        };
        request.Headers.Add("Idempotency-Key", key);
        request.Headers.Add("X-Simulator-Event-Type", "contract");
        request.Headers.Add("X-Contract-External-Reference", "SIM-12345");

        // Act
        using var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
