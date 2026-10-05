using FluentAssertions;
using NSubstitute;
using MVFC.Veragi.Simulator.Domain.Entities;
using MVFC.Veragi.Simulator.Domain.Services.Contracts;
using MVFC.Veragi.Simulator.Domain.Tests.Infrastructure;
using MVFC.Veragi.Simulator.Shareable.Enums;
using MVFC.Veragi.Simulator.Shareable.Extensions;
using MVFC.Veragi.Simulator.Shareable.Requests.Schedules;
using MVFC.Veragi.Simulator.Shareable.Requests.Simulation;
using MVFC.Veragi.Simulator.Shareable.Responses.Schedules;
using MVFC.Veragi.Simulator.Shareable.Responses.Webhooks;
using MVFC.Veragi.Simulator.TestHelpers;
using Xunit;

namespace MVFC.Veragi.Simulator.Domain.Tests.Contracts;

public sealed class AgendaReservationConsistencyTests
{
    [Theory]
    [InlineData(ContractStatusType.Active, ScheduleWebhookSchema.Schedule)]
    [InlineData(ContractStatusType.Active, ScheduleWebhookSchema.ContractReceivables)]
    [InlineData(ContractStatusType.Settled, ScheduleWebhookSchema.Schedule)]
    [InlineData(ContractStatusType.Settled, ScheduleWebhookSchema.ContractReceivables)]
    public async Task AutomaticUpdateKeepsPendingReservationsAndMatchesExplicitRefresh(ContractStatusType registeredStatus, ScheduleWebhookSchema schema)
    {
        // Arrange
        using var fixture = new ServiceFixture(MockEntities.Options() with { ScheduleWebhookSchema = schema });
        var agendaId = await fixture.PrepareAsync();
        var pending = await fixture.ContractService.CreateAsync(fixture.Contract(600), ServiceFixture.Key(), CancellationToken.None);
        var registered = await fixture.ContractService.CreateAsync(fixture.Contract(200), ServiceFixture.Key(), CancellationToken.None);
        var contracts = fixture.Operations.Where(item => item.Kind == "contract").ToArray();
        contracts[0].CreatedAt = fixture.Clock.GetUtcNow().UtcDateTime.AddSeconds(-2);
        contracts[1].CreatedAt = fixture.Clock.GetUtcNow().UtcDateTime.AddSeconds(-1);
        await fixture.ScenarioService.ConfigureAsync("contract-process", new SimulationScenarioRequest(ContractStatuses: [ContractStatusType.PendingRegistration, registeredStatus]), CancellationToken.None);

        // Act
        await fixture.Processor.ProcessAsync("contract", true, CancellationToken.None);

        // Assert
        var agenda = (await fixture.ScheduleService.GetAsync(agendaId.ToString(), CancellationToken.None)).Value!;
        FreeAmount(agenda).Should().Be(200);
        (await fixture.ContractService.GetAsync(MockEntities.MerchantCnpj, pending.Value![0].ExternalReference!, CancellationToken.None)).Value!.Status.Should().Be(ContractStatusType.PendingRegistration);
        var registeredDetails = (await fixture.ContractService.GetAsync(MockEntities.MerchantCnpj, registered.Value![0].ExternalReference!, CancellationToken.None)).Value!;
        registeredDetails.Status.Should().Be(registeredStatus);
        registeredDetails.ReachedGuarantees![0].Acquirers![0].PaymentArrangements![0].ReceivableUnits![0].ReachedAmount.Should().Be(200);
        var available = await new ContractBalanceService(fixture.Store).GetReachedAsync(fixture.Contract(1000), null, true, CancellationToken.None);
        available.Values.Sum().Should().Be(200);
        WebhookFreeAmount(fixture.Deliveries.Last(item => item.Kind == "schedule" && item.OperationId == agendaId), schema).Should().Be(200);

        await fixture.Processor.RepublishScheduleAsync(agendaId, CancellationToken.None);
        var refreshed = (await fixture.ScheduleService.GetAsync(agendaId.ToString(), CancellationToken.None)).Value!;
        refreshed.ScheduleQueryData!.Acquirers.Should().BeEquivalentTo(agenda.ScheduleQueryData!.Acquirers);
    }

    [Theory]
    [InlineData(ContractStatusType.Cancelled, ScheduleWebhookSchema.Schedule)]
    [InlineData(ContractStatusType.Cancelled, ScheduleWebhookSchema.ContractReceivables)]
    [InlineData(ContractStatusType.ContractSimulation, ScheduleWebhookSchema.Schedule)]
    [InlineData(ContractStatusType.ContractSimulation, ScheduleWebhookSchema.ContractReceivables)]
    public async Task TerminalOutcomeReleasesReservationIncludedInFreshAgenda(ContractStatusType status, ScheduleWebhookSchema schema)
    {
        // Arrange
        using var fixture = new ServiceFixture(MockEntities.Options() with { ScheduleWebhookSchema = schema });
        var originalId = await fixture.PrepareAsync();
        await fixture.ContractService.CreateAsync(fixture.Contract(600), ServiceFixture.Key(), CancellationToken.None);
        var fresh = await fixture.ScheduleService.SubmitAsync(new ScheduleQueryRequest(MockEntities.MerchantCnpj, ScheduleQueryType.STANDARD), ServiceFixture.Key(), CancellationToken.None);
        var freshId = Guid.Parse(fresh.Value!.RequestId!);
        await fixture.Processor.ProcessAsync("schedule", true, CancellationToken.None);
        FreeAmount((await fixture.ScheduleService.GetAsync(freshId.ToString(), CancellationToken.None)).Value!).Should().Be(400);
        var originalSnapshot = fixture.Operations.Single(item => item.Id == originalId).ResultJson;
        await fixture.ScenarioService.ConfigureAsync("contract-process", new SimulationScenarioRequest(ContractStatuses: [status]), CancellationToken.None);

        // Act
        await fixture.Processor.ProcessAsync("contract", true, CancellationToken.None);

        // Assert
        var agenda = (await fixture.ScheduleService.GetAsync(freshId.ToString(), CancellationToken.None)).Value!;
        FreeAmount(agenda).Should().Be(1000);
        fixture.Operations.Single(item => item.Id == originalId).ResultJson.Should().Be(originalSnapshot);
        fixture.Deliveries.Count(item => item.Kind == "schedule" && item.OperationId == originalId).Should().Be(1);
        fixture.Deliveries.Count(item => item.Kind == "schedule" && item.OperationId == freshId).Should().Be(2);
        WebhookFreeAmount(fixture.Deliveries.Last(item => item.Kind == "schedule" && item.OperationId == freshId), schema).Should().Be(1000);
        var admitted = await fixture.ContractService.CreateAsync(fixture.Contract(1000), ServiceFixture.Key(), CancellationToken.None);
        admitted.IsSuccess.Should().BeTrue();
    }

    [Theory]
    [InlineData(ContractStatusType.Cancelled)]
    [InlineData(ContractStatusType.ContractSimulation)]
    public async Task TerminalOutcomeWithoutAgendaReservationKeepsSnapshotAndDoesNotPublishRedundantUpdate(ContractStatusType status)
    {
        // Arrange
        using var fixture = new ServiceFixture();
        var agendaId = await fixture.PrepareAsync();
        var snapshot = fixture.Operations.Single(item => item.Id == agendaId).ResultJson;
        await fixture.ContractService.CreateAsync(fixture.Contract(600), ServiceFixture.Key(), CancellationToken.None);
        await fixture.ScenarioService.ConfigureAsync("contract-process", new SimulationScenarioRequest(ContractStatuses: [status]), CancellationToken.None);
        fixture.Clock.GetUtcNow().Returns(fixture.Clock.GetUtcNow().AddMinutes(1));

        // Act
        await fixture.Processor.ProcessAsync("contract", true, CancellationToken.None);

        // Assert
        fixture.Operations.Single(item => item.Id == agendaId).ResultJson.Should().Be(snapshot);
        fixture.Deliveries.Count(item => item.Kind == "schedule").Should().Be(1);
        FreeAmount((await fixture.ScheduleService.GetAsync(agendaId.ToString(), CancellationToken.None)).Value!).Should().Be(1000);
    }

    private static decimal? FreeAmount(ScheduleQuery agenda) => agenda.ScheduleQueryData!.Acquirers![0].PaymentArrangements![0].ReceivableUnits![0].FreeAmount;

    private static decimal? WebhookFreeAmount(WebhookDeliveryEntity delivery, ScheduleWebhookSchema schema) =>
        schema == ScheduleWebhookSchema.Schedule
            ? delivery.Payload.FromJson<ScheduleWebhookNotification>()!.DadosConsultaAgenda!.Credenciadoras![0].ArranjosPagamento![0].UnidadesRecebiveis![0].ValorLivre
            : delivery.Payload.FromJson<ContractWebhookNotification>()!.ScheduleQueryData.Acquirers![0].PaymentArrangements![0].ReceivableUnits![0].FreeAmount;
}
