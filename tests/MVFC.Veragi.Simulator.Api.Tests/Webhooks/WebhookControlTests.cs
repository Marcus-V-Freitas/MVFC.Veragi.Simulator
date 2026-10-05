using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using MVFC.Veragi.Simulator.Api.Tests.Infrastructure;
using MVFC.Veragi.Simulator.Domain.Ports;
using MVFC.Veragi.Simulator.Domain.Services.Webhooks;
using MVFC.Veragi.Simulator.Shareable.Enums;
using MVFC.Veragi.Simulator.Shareable.Extensions;
using MVFC.Veragi.Simulator.Shareable.Requests.Merchants;
using MVFC.Veragi.Simulator.Shareable.Requests.Schedules;
using MVFC.Veragi.Simulator.Shareable.Requests.Simulation;
using MVFC.Veragi.Simulator.Shareable.Requests.Webhooks;
using MVFC.Veragi.Simulator.Shareable.Responses.Schedules;
using MVFC.Veragi.Simulator.Shareable.Responses.Webhooks;
using Xunit;

namespace MVFC.Veragi.Simulator.Api.Tests.Webhooks;

[Collection(SimulatorCollectionDefinition.Name)]
public sealed class WebhookControlTests(SimulatorFixture fixture) : IClassFixture<SimulatorFixture>
{
    private readonly SimulatorFixture _fixture = fixture;

    [Fact]
    public async Task DestinationsPersistAcrossScopesAndResetRestoresDefaults()
    {
        // Arrange
        var payload = new WebhookDestinationsRequest(
            ScheduleUrl: "http://localhost:5090/webhooks/card-receivables/schedules/updated",
            ContractUrl: "http://localhost:5090/webhooks/card-receivable/schedules");

        // Act & Assert
        (await _fixture.Client.PutAsJsonAsync("/_simulator/webhooks", payload)).StatusCode.Should().Be(HttpStatusCode.OK);
        var rawResponse = await _fixture.Client.GetStringAsync("/_simulator/webhooks");
        rawResponse.Should().NotContain("secret-");

        var data = await _fixture.Client.GetDataAsync<WebhookDestinationsResponse>("/_simulator/webhooks");
        data.Persisted.Should().BeTrue();

        using (var scope = _fixture.Factory.Services.CreateScope())
        {
            var destination = await scope.ServiceProvider.GetRequiredService<WebhookRoutingService>().ResolveAsync("contract", CancellationToken.None);
            destination.Should().Be(payload.ContractUrl);
        }

        (await _fixture.Client.PutAsJsonAsync("/_simulator/webhooks", payload with { ContractUrl = "file:///tmp/events" })).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        await _fixture.Client.PostAsync("/_simulator/reset", null);

        data = await _fixture.Client.GetDataAsync<WebhookDestinationsResponse>("/_simulator/webhooks");
        data.Persisted.Should().BeFalse();
    }

    [Fact]
    public async Task PendingScenarioNotifiesOnceAndAutomaticallyResumesWhenCleared()
    {
        // Arrange
        const string cnpj = "53185894000174";
        const string root = "/module/card-receivable";
        var merchant = new MerchantCreateRequest(
            Cnpj: cnpj,
            CorporateName: "Pending Test",
            LimitType: LimitType.Transactional,
            ReceivablesScheduleConfig: new ReceivablesScheduleConfig(
                QueryWindow: QueryWindowType.P6M,
                AcquirerCnpjs: ["82413081000116"],
                ArrangementCodes: ["VCC"]));

        await _fixture.Client.PostAsJsonAsync(root + "/merchants", merchant, JsonExtensions.Options);
        await _fixture.Client.PutAsJsonAsync("/_simulator/scenarios/schedule-process", new SimulationScenarioRequest(MerchantCnpj: cnpj, HoldProcessing: true));

        // Act
        var submitResponse = await _fixture.Client.PostIdempotentAsync(root + "/schedules/query-requests", new ScheduleQueryRequest(MerchantCnpj: cnpj, QueryType: ScheduleQueryType.STANDARD));
        var submitted = await submitResponse.ReadDataAsync<ScheduleQueryResponse>();
        var id = submitted.RequestId!;

        for (var cycle = 0; cycle < 3; cycle++)
        {
            await _fixture.ProcessAsync("schedule");
        }

        // Assert
        using (var scope = _fixture.Factory.Services.CreateScope())
        {
            var deliveries = await scope.ServiceProvider.GetRequiredService<ISimulatorStore>().GetDeliveriesAsync(CancellationToken.None);
            deliveries.Should().ContainSingle(x => x.MerchantCnpj == cnpj);
        }

        var result = await _fixture.Client.GetDataAsync<ScheduleQuery>(root + "/schedules/query-requests/" + id);
        result.Status.Should().Be(ScheduleQueryStatusType.PROCESSING);

        await _fixture.Client.DeleteAsync("/_simulator/scenarios/schedule-process?merchantCnpj=" + cnpj);
        await _fixture.ProcessAsync("schedule");

        result = await _fixture.Client.GetDataAsync<ScheduleQuery>(root + "/schedules/query-requests/" + id);
        result.Status.Should().Be(ScheduleQueryStatusType.PROCESSED);
    }
}
