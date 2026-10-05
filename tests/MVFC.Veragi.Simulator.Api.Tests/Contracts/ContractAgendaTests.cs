using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MVFC.Veragi.Simulator.Api.Tests.Infrastructure;
using MVFC.Veragi.Simulator.Data;
using MVFC.Veragi.Simulator.Shareable.Enums;
using MVFC.Veragi.Simulator.Shareable.Extensions;
using MVFC.Veragi.Simulator.Shareable.Requests.Contracts;
using MVFC.Veragi.Simulator.Shareable.Requests.Merchants;
using MVFC.Veragi.Simulator.Shareable.Requests.Reconciliation;
using MVFC.Veragi.Simulator.Shareable.Requests.Sales;
using MVFC.Veragi.Simulator.Shareable.Requests.Schedules;
using MVFC.Veragi.Simulator.Shareable.Requests.Simulation;
using MVFC.Veragi.Simulator.Shareable.Responses.Contracts;
using MVFC.Veragi.Simulator.Shareable.Responses.Schedules;
using MVFC.Veragi.Simulator.Shareable.Responses.Simulation;
using MVFC.Veragi.Simulator.Shareable.Responses.Webhooks;
using Xunit;

namespace MVFC.Veragi.Simulator.Api.Tests.Contracts;

[Collection(SimulatorCollectionDefinition.Name)]
public sealed class ContractAgendaTests(SimulatorFixture fixture) : IClassFixture<SimulatorFixture>
{
    private readonly SimulatorFixture _fixture = fixture;
    private const string Root = "/module/card-receivable";
    private const string Cnpj = "44185894000174";

    [Fact]
    public async Task PartialAndFullAnticipationCommitOnlyReachedAmountsAndUseContractAgendaWebhook()
    {
        // Arrange
        var (initialAgendaId, date) = await PrepareAsync();
        var first = await ContractAsync(300, date);

        // Act
        await _fixture.ProcessAsync("contract");

        // Assert
        var firstDetails = await DetailsAsync(first);
        firstDetails.ReachedGuarantees![0].Acquirers![0].PaymentArrangements![0].ReceivableUnits![0].ReachedAmount.Should().Be(300m);

        var agenda = await SubmitAgendaAsync();
        await _fixture.ProcessAsync("schedule");
        (await UnitAsync(agenda)).FreeAmount.Should().Be(700m);

        var second = await ContractAsync(700, date);
        await _fixture.ProcessAsync("contract");
        var secondDetails = await DetailsAsync(second);
        secondDetails.ReachedGuarantees![0].Acquirers![0].PaymentArrangements![0].ReceivableUnits![0].ReachedAmount.Should().Be(700m);

        agenda = await SubmitAgendaAsync();
        await _fixture.ProcessAsync("schedule");
        (await UnitAsync(agenda)).FreeAmount.Should().Be(0m);

        using var scope = _fixture.Factory.Services.CreateScope();
        var deliveries = await scope.ServiceProvider.GetRequiredService<SimulatorDbContext>().Deliveries
            .Where(x => x.Kind == "contract").ToListAsync();
        var delivery = deliveries.Should().ContainSingle(x => x.ExternalReference == first).Which;

        var payload = delivery.Payload.FromJson<ContractWebhookNotification>()!;
        payload.Status.Should().Be(ScheduleQueryStatusType.PROCESSED);
        payload.ScheduleQueryData.RequestId.Should().Be(delivery.OperationId.ToString());

        var unit = payload.ScheduleQueryData.Acquirers![0].PaymentArrangements![0].ReceivableUnits![0];
        unit.TotalAmount.Should().Be(300m);
        unit.FreeAmount.Should().Be(0m);
    }

    [Fact]
    public async Task PriorAnticipationReducesReachedAmountAndCancellationDoesNotConsumeBalance()
    {
        // Arrange
        var (_, date) = await PrepareAsync();
        var external = new ExternalAnticipationRequest(
            AcquirerCnpj: "82413081000116",
            PaymentArrangementCode: "VCC",
            SettlementDate: date,
            Amount: 400m);

        // Act
        var key = Guid.CreateVersion7(DateTimeOffset.UtcNow).ToString();

        // Assert
        (await _fixture.Client.PostIdempotentAsync("/_simulator/merchants/" + Cnpj + "/external-anticipations", external, key)).StatusCode.Should().Be(HttpStatusCode.Created);
        (await _fixture.Client.PostIdempotentAsync("/_simulator/merchants/" + Cnpj + "/external-anticipations", external, key)).StatusCode.Should().Be(HttpStatusCode.Created);
        await _fixture.Client.ConfigureScenarioAsync("contract-process", new SimulationScenarioRequest(MerchantCnpj: Cnpj, ContractStatuses: [ContractStatusType.Cancelled, ContractStatusType.Active]));

        var cancelled = await ContractAsync(1000, date);
        await _fixture.ProcessAsync("contract");
        (await DetailsAsync(cancelled)).Status.Should().Be(ContractStatusType.Cancelled);

        var completed = await ContractAsync(1000, date);
        await _fixture.ProcessAsync("contract");
        var completedDetails = await DetailsAsync(completed);
        completedDetails.DebtBalanceAmount.Should().Be(600m);

        var unit = completedDetails.ReachedGuarantees![0].Acquirers![0].PaymentArrangements![0].ReceivableUnits![0];
        unit.RequestedAmount.Should().Be(1000m);
        unit.ReachedAmount.Should().Be(600m);

        using var scope = _fixture.Factory.Services.CreateScope();
        (await scope.ServiceProvider.GetRequiredService<SimulatorDbContext>().ExternalAnticipations.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task ScenarioCountersAndIntermediateStatusesProduceDistinctDurableEventsAndResetClearsEnvironment()
    {
        // Arrange
        var (_, date) = await PrepareAsync();
        await _fixture.Client.ConfigureScenarioAsync("schedule-query", new SimulationScenarioRequest(MerchantCnpj: Cnpj, FailuresRemaining: 2, FailureStatusCode: 503));

        // Act
        var agenda = await SubmitAgendaAsync();

        // Assert
        (await _fixture.Client.GetAsync(Root + "/schedules/query-requests/" + agenda)).StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        (await _fixture.Client.GetAsync(Root + "/schedules/query-requests/" + agenda)).StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        (await _fixture.Client.GetAsync(Root + "/schedules/query-requests/" + agenda)).StatusCode.Should().Be(HttpStatusCode.OK);
        await _fixture.Client.ConfigureScenarioAsync("schedule-process", new SimulationScenarioRequest(MerchantCnpj: Cnpj, ProcessingOutcomes: [ScheduleQueryStatusType.PROCESSING, ScheduleQueryStatusType.PROCESSED]));

        await _fixture.ProcessAsync("schedule");
        var processingQuery = await _fixture.Client.GetDataAsync<ScheduleQuery>(Root + "/schedules/query-requests/" + agenda);
        processingQuery.Status.Should().Be(ScheduleQueryStatusType.PROCESSING);

        await _fixture.ProcessAsync("schedule");
        var processedQuery = await _fixture.Client.GetDataAsync<ScheduleQuery>(Root + "/schedules/query-requests/" + agenda);
        processedQuery.Status.Should().Be(ScheduleQueryStatusType.PROCESSED);

        await _fixture.Client.ConfigureScenarioAsync("contract-process", new SimulationScenarioRequest(MerchantCnpj: Cnpj, ContractStatuses: [ContractStatusType.PendingRegistration, ContractStatusType.PendingEdit, ContractStatusType.Active]));

        var contract = await ContractAsync(100, date);

        foreach (var expected in new[] { ContractStatusType.PendingRegistration, ContractStatusType.PendingEdit, ContractStatusType.Active })
        {
            await _fixture.ProcessAsync("contract");
            (await DetailsAsync(contract)).Status.Should().Be(expected);
        }

        using (var scope = _fixture.Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SimulatorDbContext>();
            var scenario = await db.Scenarios.SingleAsync(x => x.Target == "schedule-query");
            scenario.Calls.Should().Be(5);
            scenario.FailuresRemaining.Should().Be(0);
            (await db.Deliveries.CountAsync(x => x.Kind == "contract")).Should().Be(3);
        }

        var reset = await _fixture.Client.PostAsync("/_simulator/reset", null);
        reset.StatusCode.Should().Be(HttpStatusCode.OK);
        var resetData = await reset.ReadDataAsync<EnvironmentResetResponse>();
        resetData.RemovedDocuments.Should().BeGreaterThan(0);
        (await _fixture.Client.GetAsync(Root + "/merchants/" + Cnpj)).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await _fixture.Client.GetAsync(Root + "/bases-control/acquirers")).StatusCode.Should().Be(HttpStatusCode.OK);

        using var emptyScope = _fixture.Factory.Services.CreateScope();
        var empty = emptyScope.ServiceProvider.GetRequiredService<SimulatorDbContext>();
        (await empty.Scenarios.CountAsync()).Should().Be(0);
        (await empty.Operations.CountAsync()).Should().Be(0);
        (await empty.Deliveries.CountAsync()).Should().Be(0);
        (await empty.Sales.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task PartialSuccessUpdatesExistingAgendaAndWebhookWhileFailedConcurrentContractDoesNotConsumeOtherBalance()
    {
        // Arrange
        var (initialAgendaId, date) = await PrepareAsync();
        await _fixture.Client.ConfigureScenarioAsync("contract-process", new SimulationScenarioRequest(MerchantCnpj: Cnpj, ContractStatuses: [ContractStatusType.Active, ContractStatusType.Cancelled]));

        // Act
        var accepted = await ContractAsync(300, date);
        var rejected = await ContractAsync(500, date);
        await _fixture.ProcessAsync("contract");

        // Assert
        (await DetailsAsync(accepted)).Status.Should().Be(ContractStatusType.Active);
        (await DetailsAsync(rejected)).Status.Should().Be(ContractStatusType.Cancelled);
        (await UnitAsync(initialAgendaId)).FreeAmount.Should().Be(700m);

        var agenda = await _fixture.Client.GetDataAsync<ScheduleQuery>(Root + "/schedules/query-requests/" + initialAgendaId);
        var units = agenda.ScheduleQueryData!.Acquirers![0].PaymentArrangements![0].ReceivableUnits!;
        units[1].FreeAmount.Should().Be(1000m);

        using (var scope = _fixture.Factory.Services.CreateScope())
        {
            var deliveries = await scope.ServiceProvider.GetRequiredService<SimulatorDbContext>().Deliveries.Where(x => x.Kind == "schedule").ToListAsync();
            deliveries.Count.Should().Be(2);

            var update = deliveries.Should().ContainSingle(x =>
                x.Payload.FromJson<ScheduleWebhookNotification>()!
                    .DadosConsultaAgenda!.Credenciadoras![0].ArranjosPagamento![0].UnidadesRecebiveis![0].ValorLivre == 700m).Which;
            update.OperationId.Should().Be(Guid.Parse(initialAgendaId));
        }

        await _fixture.ProcessAsync("contract");
        (await UnitAsync(initialAgendaId)).FreeAmount.Should().Be(700m);

        var completed = await ContractAsync(700, date);
        await _fixture.ProcessAsync("contract");
        (await DetailsAsync(completed)).DebtBalanceAmount.Should().Be(700m);
        (await UnitAsync(initialAgendaId)).FreeAmount.Should().Be(0m);
    }

    [Fact]
    public async Task PendingCancelledAndSimulationContractsDoNotChangeExistingAgendaOrEmitAgendaUpdate()
    {
        // Arrange
        var (initialAgendaId, date) = await PrepareAsync();
        var before = (await UnitAsync(initialAgendaId)).ToJson();
        await _fixture.Client.ConfigureScenarioAsync("contract-process", new SimulationScenarioRequest(MerchantCnpj: Cnpj, ContractStatuses: [ContractStatusType.PendingRegistration, ContractStatusType.Cancelled]));
        var contract = await ContractAsync(500, date);

        // Act
        await _fixture.ProcessAsync("contract");

        // Assert
        (await DetailsAsync(contract)).Status.Should().Be(ContractStatusType.PendingRegistration);
        (await UnitAsync(initialAgendaId)).ToJson().Should().Be(before);

        await _fixture.ProcessAsync("contract");
        (await DetailsAsync(contract)).Status.Should().Be(ContractStatusType.Cancelled);
        (await UnitAsync(initialAgendaId)).ToJson().Should().Be(before);

        await _fixture.Client.ConfigureScenarioAsync("contract-process", new SimulationScenarioRequest(MerchantCnpj: Cnpj, ContractStatuses: [ContractStatusType.ContractSimulation]));
        await ContractAsync(500, date);
        await _fixture.ProcessAsync("contract");
        (await UnitAsync(initialAgendaId)).ToJson().Should().Be(before);

        using var scope = _fixture.Factory.Services.CreateScope();
        (await scope.ServiceProvider.GetRequiredService<SimulatorDbContext>().Deliveries.CountAsync(x => x.Kind == "schedule")).Should().Be(1);
    }

    private async Task<(string InitialAgendaId, string SettlementDate)> PrepareAsync()
    {
        var reset = await _fixture.Client.PostAsync("/_simulator/reset", null);
        reset.StatusCode.Should().Be(HttpStatusCode.OK);

        var merchant = new MerchantCreateRequest(
            Cnpj: Cnpj,
            CorporateName: "Contract Agenda Test",
            LimitType: LimitType.Transactional,
            CreditConfigurations: [CreditConfigurationType.OccasionalAnticipation],
            ReceivablesScheduleConfig: new ReceivablesScheduleConfig(
                QueryWindow: QueryWindowType.P6M,
                AcquirerCnpjs: ["82413081000116"],
                ArrangementCodes: ["VCC"]),
            AnticipationSettlementAccount: new SettlementAccountCreateRequest(
                CnpjRecipient: Cnpj,
                AccountType: AccountType.CONTA_DEPOSITO_A_VISTA,
                BankCode: "341",
                Ispb: "60746948",
                Branch: "1234",
                Account: "9999"));

        (await _fixture.Client.PostAsJsonAsync(Root + "/merchants", merchant, JsonExtensions.Options)).StatusCode.Should().Be(HttpStatusCode.Created);
        (await _fixture.Client.PostIdempotentAsync("/_simulator/merchants/" + Cnpj + "/sales/generate", new GenerateSalesRequest(Days: 2, SalesPerDay: 1, AmountPerSale: 1000m))).StatusCode.Should().Be(HttpStatusCode.Created);

        var agenda = await SubmitAgendaAsync();
        await _fixture.ProcessAsync("schedule");

        var unit = await UnitAsync(agenda);
        return (agenda, unit.SettlementDate!);
    }

    private async Task<string> SubmitAgendaAsync()
    {
        var response = await _fixture.Client.PostIdempotentAsync(Root + "/schedules/query-requests", new ScheduleQueryRequest(MerchantCnpj: Cnpj, QueryType: ScheduleQueryType.STANDARD));
        var data = await response.ReadDataAsync<ScheduleQueryResponse>();
        return data.RequestId!;
    }

    private async Task<string> ContractAsync(decimal amount, string date)
    {
        var request = new ContractAnticipationCreateRequest(
            ContractorCnpj: Cnpj,
            FinancierContractId: Guid.CreateVersion7(DateTimeOffset.UtcNow).ToString(),
            SignatureDate: DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            RequestedAmount: amount,
            Guarantees:
            [
                new ContractAnticipationGuaranteeCreate(
                    ReceivableUnitHolderCnpj: Cnpj,
                    FinalUserReceiverCnpj: Cnpj,
                    AcquirerCnpj: "82413081000116",
                    PaymentArrangementCode: "VCC",
                    SettlementDate: date,
                    DefinedAmount: amount)
            ]);

        var result = await _fixture.Client.PostIdempotentAsync(Root + "/contracts/anticipation", request);
        result.StatusCode.Should().Be(HttpStatusCode.Created);

        var contracts = await result.ReadDataAsync<IReadOnlyList<ContractByContractorAndSituation>>();
        return contracts[0].ExternalReference!;
    }

    private async Task<ScheduleReceivableUnit> UnitAsync(string agenda)
    {
        var query = await _fixture.Client.GetDataAsync<ScheduleQuery>(Root + "/schedules/query-requests/" + agenda);
        return query.ScheduleQueryData!.Acquirers![0].PaymentArrangements![0].ReceivableUnits![0];
    }

    private async Task<ContractByExternalReference> DetailsAsync(string reference)
    {
        return await _fixture.Client.GetDataAsync<ContractByExternalReference>(
            Root + "/contracts/by-external-reference?contractorCnpj=" + Cnpj + "&externalReference=" + reference);
    }
}
