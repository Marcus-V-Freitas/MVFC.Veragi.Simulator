using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using MVFC.Veragi.Simulator.Api.Tests.Infrastructure;
using MVFC.Veragi.Simulator.Domain.Services.Common;
using MVFC.Veragi.Simulator.Shareable.Enums;
using MVFC.Veragi.Simulator.Shareable.Extensions;
using MVFC.Veragi.Simulator.Shareable.Requests.Contracts;
using MVFC.Veragi.Simulator.Shareable.Requests.Sales;
using MVFC.Veragi.Simulator.Shareable.Requests.Schedules;
using MVFC.Veragi.Simulator.Shareable.Responses.Common;
using MVFC.Veragi.Simulator.Shareable.Responses.Contracts;
using MVFC.Veragi.Simulator.Shareable.Responses.Schedules;
using MVFC.Veragi.Simulator.Shareable.Responses.Webhooks;
using MVFC.Veragi.Simulator.TestHelpers;
using Xunit;

namespace MVFC.Veragi.Simulator.Api.Tests.Contracts;

[Collection(SimulatorCollectionDefinition.Name)]
public sealed class ContractAvailabilityFlowTests(SimulatorFixture fixture) : IClassFixture<SimulatorFixture>
{
    private readonly SimulatorFixture _fixture = fixture;
    private const string ContractRoute = "/module/card-receivable/contracts/anticipation";
    private const string AvailabilityRoute = "/_simulator/contracts/availability";

    [Fact]
    public async Task PartialThenRemainingAnticipationsExhaustUnitAndNextRequestFailsImmediately()
    {
        // Arrange
        var (request, scheduleId) = await PrepareAsync(250);
        await _fixture.Client.PostIdempotentAsync(ContractRoute, request);
        await _fixture.Client.PostAsync("/_simulator/run", null);

        // Act
        var remaining = request with
        {
            RequestedAmount = 750,
            Guarantees = [request.Guarantees![0] with
            {
                DefinedAmount = 750
            }]
        };
        var accepted = await _fixture.Client.PostIdempotentAsync(ContractRoute, remaining);
        await _fixture.Client.PostAsync("/_simulator/run", null);
        var rejected = await _fixture.Client.PostIdempotentAsync(ContractRoute, request);
        var agenda = await _fixture.Client.GetDataAsync<ScheduleQuery>("/module/card-receivable/schedules/query-requests/" + scheduleId);

        // Assert
        accepted.StatusCode.Should().Be(HttpStatusCode.Created);
        rejected.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var problem = await rejected.Content.ReadFromJsonAsync<ProblemDetails>(JsonExtensions.Options);
        problem!.ErrorCode.Should().Be("INSUFFICIENT_RECEIVABLE_BALANCE");
        agenda.ScheduleQueryData!.Acquirers![0].PaymentArrangements![0].ReceivableUnits![0].FreeAmount.Should().Be(0);
    }

    [Fact]
    public async Task DeferredModeProducesErrorWebhookAndDoesNotUpdateExhaustedAgenda()
    {
        // Arrange
        var (request, _) = await PrepareAsync(1000);
        await _fixture.Client.PostIdempotentAsync(ContractRoute, request);
        await _fixture.Client.PostAsync("/_simulator/run", null);
        await _fixture.Client.PutAsJsonAsync(AvailabilityRoute, new ContractAvailabilityRequest(ContractAvailabilityMode.DEFERRED), JsonExtensions.Options);
        var before = (await _fixture.Client.GetFromJsonAsync<IReadOnlyList<WebhookDeliveryResponse>>("/_simulator/deliveries", JsonExtensions.Options))!;
        var scheduleEvents = before.Count(item => item.Kind == "schedule");

        // Act
        var accepted = await _fixture.Client.PostIdempotentAsync(ContractRoute, request);
        var summary = (await accepted.ReadDataAsync<IReadOnlyList<ContractByContractorAndSituation>>())[0];
        var reference = summary.ExternalReference!;
        await _fixture.Client.PostAsync("/_simulator/run", null);
        var details = await _fixture.Client.GetDataAsync<ContractByExternalReference>("/module/card-receivable/contracts/by-external-reference?contractorCnpj=" + MockEntities.MerchantCnpj + "&externalReference=" + reference);
        var deliveries = (await _fixture.Client.GetFromJsonAsync<IReadOnlyList<WebhookDeliveryResponse>>("/_simulator/deliveries", JsonExtensions.Options))!;

        // Assert
        accepted.StatusCode.Should().Be(HttpStatusCode.Created);
        details.Status.Should().Be(ContractStatusType.Cancelled);
        deliveries.Count(item => item.Kind == "schedule").Should().Be(scheduleEvents);
        var errorEvent = deliveries.Single(item => item.ExternalReference == reference);
        errorEvent.Payload!["status"]!.GetValue<string>().Should().Be("ERROR");
    }

    [Fact]
    public async Task PolicySurvivesApplicationRestartAndResetRestoresSettingsDefault()
    {
        // Arrange
        await _fixture.Client.PostAsync("/_simulator/reset", null);
        await _fixture.Client.PutAsJsonAsync(AvailabilityRoute, new ContractAvailabilityRequest(ContractAvailabilityMode.DEFERRED), JsonExtensions.Options);
        await using var restarted = new SimulatorWebApplicationFactory(_fixture.ConnectionString, _fixture.DatabaseName);
        using var client = restarted.CreateClient();

        // Act
        var persisted = await client.GetDataAsync<ContractAvailabilityResponse>(AvailabilityRoute);
        var reset = await client.PostAsync("/_simulator/reset", null);
        var restored = await client.GetDataAsync<ContractAvailabilityResponse>(AvailabilityRoute);

        // Assert
        persisted.Mode.Should().Be(ContractAvailabilityMode.DEFERRED);
        persisted.Persisted.Should().BeTrue();
        reset.StatusCode.Should().Be(HttpStatusCode.OK);
        restored.Mode.Should().Be(ContractAvailabilityMode.IMMEDIATE);
        restored.Persisted.Should().BeFalse();
    }

    [Fact]
    public async Task BindingFailureReturnsProblemWithoutAuthentication()
    {
        // Arrange
        using var message = new HttpRequestMessage(HttpMethod.Put, AvailabilityRoute)
        {
            Content = JsonContent.Create(new { mode = "UNKNOWN" })
        };

        // Act
        var response = await _fixture.Client.SendAsync(message);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(JsonExtensions.Options);
        problem!.ErrorCode.Should().Be("VALIDATION_FAILED");
    }

    private async Task<(
        ContractAnticipationCreateRequest Request,
        string ScheduleId
    )> PrepareAsync(decimal amount)
    {
        await _fixture.Client.PostAsync("/_simulator/reset", null);
        await _fixture.Client.PostAsJsonAsync("/module/card-receivable/merchants", MockEntities.MerchantRequest(), JsonExtensions.Options);
        await _fixture.Client.PostIdempotentAsync(
            "/_simulator/merchants/" + MockEntities.MerchantCnpj + "/sales/generate",
            new GenerateSalesRequest(Days: 1, SalesPerDay: 1, AmountPerSale: 1000));
        var submitResponse = await _fixture.Client.PostIdempotentAsync(
            "/module/card-receivable/schedules/query-requests",
            new ScheduleQueryRequest(MockEntities.MerchantCnpj, ScheduleQueryType.STANDARD));
        var scheduleId = (await submitResponse.ReadDataAsync<ScheduleQueryResponse>()).RequestId!;
        await _fixture.Client.PostAsync("/_simulator/run", null);
        var agenda = await _fixture.Client.GetDataAsync<ScheduleQuery>("/module/card-receivable/schedules/query-requests/" + scheduleId);
        var unit = agenda.ScheduleQueryData!.Acquirers![0].PaymentArrangements![0].ReceivableUnits![0];
        var today = new BusinessCalendar(TimeProvider.System, MockEntities.Options()).Today;
        var request = MockEntities.Contract(today, DateOnly.Parse(unit.SettlementDate!, CultureInfo.InvariantCulture), amount);

        return (request, scheduleId);
    }
}
