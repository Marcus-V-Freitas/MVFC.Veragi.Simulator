using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using MVFC.Veragi.Simulator.Api.Tests.Infrastructure;
using MVFC.Veragi.Simulator.Shareable.Enums;
using MVFC.Veragi.Simulator.Shareable.Extensions;
using MVFC.Veragi.Simulator.Shareable.Requests.Schedules;
using MVFC.Veragi.Simulator.Shareable.Responses.Simulation;
using MVFC.Veragi.Simulator.Shareable.Responses.Webhooks;
using MVFC.Veragi.Simulator.TestHelpers;
using Xunit;

namespace MVFC.Veragi.Simulator.Api.Tests.Simulation;

[Collection(SimulatorCollectionDefinition.Name)]
public sealed class SimulationInspectionTests(SimulatorFixture fixture) : IClassFixture<SimulatorFixture>
{
    private readonly SimulatorFixture _fixture = fixture;

    [Fact]
    public async Task RunProcessesScheduleAndInspectionRoutesRemainAvailable()
    {
        // Arrange
        await _fixture.Client.PostAsJsonAsync("/module/card-receivable/merchants", MockEntities.MerchantRequest(), JsonExtensions.Options);
        await _fixture.Client.PostIdempotentAsync("/module/card-receivable/schedules/query-requests", new ScheduleQueryRequest(MockEntities.MerchantCnpj, ScheduleQueryType.STANDARD));

        // Act
        var run = await _fixture.Client.PostAsync("/_simulator/run", null);
        var scenarios = await _fixture.Client.GetAsync("/_simulator/scenarios");
        var targets = await _fixture.Client.GetAsync("/_simulator/scenario-targets");
        var deliveries = await _fixture.Client.GetAsync("/_simulator/deliveries?kind=schedule&merchantCnpj=" + MockEntities.MerchantCnpj);

        // Assert
        run.StatusCode.Should().Be(HttpStatusCode.OK);
        var runData = await run.ReadDataAsync<SimulationRunResponse>();
        runData.Should().NotBeNull();

        scenarios.StatusCode.Should().Be(HttpStatusCode.OK);
        targets.StatusCode.Should().Be(HttpStatusCode.OK);
        deliveries.StatusCode.Should().Be(HttpStatusCode.OK);
        var deliveryList = await deliveries.ReadJsonAsync<IReadOnlyList<WebhookDeliveryResponse>>();
        deliveryList.Should().NotBeNull();
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("00000000-0000-0000-0000-000000000001")]
    public async Task ReplayMissingDeliveryReturnsNotFound(string id)
    {
        // Arrange
        var route = "/_simulator/deliveries/" + id + "/replay";

        // Act
        var response = await _fixture.Client.PostAsync(route, null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeliberatelyFailingWebhookReturnsServiceUnavailable()
    {
        // Arrange
        var route = "/_simulator/webhooks/fail";

        // Act
        var response = await _fixture.Client.PostAsync(route, null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }
}
