using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using MVFC.Veragi.Simulator.Api.Tests.Infrastructure;
using MVFC.Veragi.Simulator.Shareable.Enums;
using MVFC.Veragi.Simulator.Shareable.Extensions;
using MVFC.Veragi.Simulator.Shareable.Responses.Webhooks;
using MVFC.Veragi.Simulator.TestHelpers;
using Xunit;

namespace MVFC.Veragi.Simulator.Api.Tests.Webhooks;

[Collection(SimulatorCollectionDefinition.Name)]
public sealed class ProviderWebhookReceiptTests(SimulatorFixture fixture) : IClassFixture<SimulatorFixture>
{
    private readonly SimulatorFixture _fixture = fixture;

    [Fact]
    public async Task PortugueseScheduleWebhookIsAcceptedAndCanBeInspected()
    {
        // Arrange
        var requestId = Guid.CreateVersion7(DateTimeOffset.UtcNow).ToString();
        var payload = new ScheduleWebhookNotification(
            Status: ScheduleQueryStatusType.PROCESSED,
            DadosConsultaAgenda: new ScheduleWebhookData(IdRequisicao: requestId, CnpjEstabelecimento: MockEntities.MerchantCnpj));

        // Act
        var received = await _fixture.Client.PostIdempotentAsync("/webhooks/card-receivables/schedules/updated", payload);
        var inspected = await _fixture.Client.GetAsync("/_simulator/receipts?kind=schedule&merchantCnpj=" + MockEntities.MerchantCnpj + "&requestId=" + requestId);

        // Assert
        received.StatusCode.Should().Be(HttpStatusCode.NoContent);
        inspected.StatusCode.Should().Be(HttpStatusCode.OK);
        var receipts = await inspected.ReadJsonAsync<IReadOnlyList<WebhookReceiptResponse>>();
        receipts.Should().ContainSingle(r => r.RequestId == requestId);
    }

    [Fact]
    public async Task EnglishContractWebhookIsAcceptedWithoutCredentials()
    {
        // Arrange
        var payload = new ContractWebhookNotification(
            ScheduleQueryStatusType.PROCESSED,
            "done",
            new ContractWebhookData(Guid.CreateVersion7(DateTimeOffset.UtcNow).ToString(), ScheduleQueryOriginType.ONLINE, DateTimeOffset.UtcNow.ToString("O"), MockEntities.MerchantCnpj, []));

        using var message = new HttpRequestMessage(HttpMethod.Post, "/webhooks/card-receivable/schedules")
        {
            Content = JsonContent.Create(payload, options: JsonExtensions.Options)
        };
        message.Headers.Add("Idempotency-Key", Guid.CreateVersion7(DateTimeOffset.UtcNow).ToString());
        message.Headers.Add("X-Simulator-Event-Type", "contract");

        // Act
        var response = await _fixture.Client.SendAsync(message);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
