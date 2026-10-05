using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MVFC.Veragi.Simulator.Api.Tests.Infrastructure;
using MVFC.Veragi.Simulator.Data;
using MVFC.Veragi.Simulator.Domain.Services.Common;
using MVFC.Veragi.Simulator.Domain.Services.Simulation;
using MVFC.Veragi.Simulator.Domain.Services.Webhooks;
using MVFC.Veragi.Simulator.Shareable.Configuration;
using MVFC.Veragi.Simulator.Shareable.Enums;
using MVFC.Veragi.Simulator.Shareable.Extensions;
using MVFC.Veragi.Simulator.Shareable.Requests.Contracts;
using MVFC.Veragi.Simulator.Shareable.Requests.Merchants;
using MVFC.Veragi.Simulator.Shareable.Requests.Reconciliation;
using MVFC.Veragi.Simulator.Shareable.Requests.Sales;
using MVFC.Veragi.Simulator.Shareable.Requests.Schedules;
using MVFC.Veragi.Simulator.Shareable.Responses.Catalogs;
using MVFC.Veragi.Simulator.Shareable.Responses.Contracts;
using MVFC.Veragi.Simulator.Shareable.Responses.Schedules;
using MVFC.Veragi.Simulator.TestHelpers;
using Xunit;

namespace MVFC.Veragi.Simulator.Api.Tests.Provider;

[Collection(SimulatorCollectionDefinition.Name)]
public sealed class ProviderFlowTests(SimulatorFixture fixture) : IClassFixture<SimulatorFixture>
{
    private readonly SimulatorFixture _fixture = fixture;
    private const string Root = "/module/card-receivable";

    [Fact]
    public async Task CompleteFlowPersistsAgendaContractAndReconciliationAndSupportsIdempotency()
    {
        // Arrange & Act & Assert
        _fixture.Factory.Services.GetRequiredService<SimulatorOptions>().MaxDeliveryAttempts.Should().Be(2);
        _fixture.Factory.Services.GetRequiredService<SimulatorOptions>().SchedulersEnabled.Should().BeFalse();

        var cnpj = "22185894000174";
        var merchant = new MerchantCreateRequest(
            Cnpj: cnpj,
            CorporateName: "Integration Test",
            LimitType: LimitType.Transactional,
            CreditConfigurations: [CreditConfigurationType.OccasionalAnticipation],
            ReceivablesScheduleConfig: new ReceivablesScheduleConfig(
                QueryWindow: QueryWindowType.P6M,
                AcquirerCnpjs: ["82413081000116"],
                ArrangementCodes: ["VCC"]),
            AnticipationSettlementAccount: new SettlementAccountCreateRequest(
                CnpjRecipient: cnpj,
                AccountType: AccountType.CONTA_DEPOSITO_A_VISTA,
                BankCode: "341",
                Ispb: "60746948",
                Branch: "1234",
                Account: "9999"));

        (await _fixture.Client.PostAsJsonAsync(Root + "/merchants", merchant, JsonExtensions.Options)).StatusCode.Should().Be(HttpStatusCode.Created);
        (await _fixture.Client.PostAsJsonAsync(Root + "/merchants", merchant, JsonExtensions.Options)).StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await _fixture.Client.GetAsync(Root + "/merchants/" + cnpj)).StatusCode.Should().Be(HttpStatusCode.OK);
        (await _fixture.Client.PatchAsJsonAsync(Root + "/merchants/" + cnpj, new { corporateName = "Updated Test" })).StatusCode.Should().Be(HttpStatusCode.OK);

        var salesMessage = new GenerateSalesRequest(Days: 2, SalesPerDay: 2, AmountPerSale: 5000m);
        (await _fixture.Client.PostIdempotentAsync("/_simulator/merchants/" + cnpj + "/sales/generate", salesMessage)).StatusCode.Should().Be(HttpStatusCode.Created);

        var scheduleKey = Guid.CreateVersion7(DateTimeOffset.UtcNow).ToString();
        var schedule = await _fixture.Client.PostIdempotentAsync(Root + "/schedules/query-requests", new ScheduleQueryRequest(MerchantCnpj: cnpj, QueryType: ScheduleQueryType.STANDARD), scheduleKey);
        schedule.StatusCode.Should().Be(HttpStatusCode.Accepted);

        var id = (await schedule.ReadDataAsync<ScheduleQueryResponse>()).RequestId!;
        var duplicate = await _fixture.Client.PostIdempotentAsync(Root + "/schedules/query-requests", new ScheduleQueryRequest(MerchantCnpj: cnpj, QueryType: ScheduleQueryType.STANDARD), scheduleKey);
        (await duplicate.ReadDataAsync<ScheduleQueryResponse>()).RequestId.Should().Be(id);

        (await _fixture.Client.PostIdempotentAsync(Root + "/schedules/query-requests", new ScheduleQueryRequest(MerchantCnpj: cnpj, QueryType: ScheduleQueryType.STANDARD))).StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await _fixture.Client.PostIdempotentAsync(Root + "/schedules/query-requests", new ScheduleQueryRequest(MerchantCnpj: cnpj, QueryType: ScheduleQueryType.EXPLORATORY))).StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

        var pendingQuery = await _fixture.Client.GetDataAsync<ScheduleQuery>(Root + "/schedules/query-requests/" + id);
        pendingQuery.Status.Should().Be(ScheduleQueryStatusType.PROCESSING);

        await _fixture.ProcessAsync("schedule");

        var agenda = await _fixture.Client.GetDataAsync<ScheduleQuery>(Root + "/schedules/query-requests/" + id);
        agenda.Status.Should().Be(ScheduleQueryStatusType.PROCESSED);

        var unit = agenda.ScheduleQueryData!.Acquirers![0].PaymentArrangements![0].ReceivableUnits![0];
        var request = new ContractAnticipationCreateRequest(
            ContractorCnpj: cnpj,
            FinancierContractId: "TEST-1",
            SignatureDate: DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            RequestedAmount: 100,
            Guarantees:
            [
                new ContractAnticipationGuaranteeCreate(
                    ReceivableUnitHolderCnpj: cnpj,
                    FinalUserReceiverCnpj: cnpj,
                    AcquirerCnpj: "82413081000116",
                    PaymentArrangementCode: "VCC",
                    SettlementDate: unit.SettlementDate!,
                    DefinedAmount: 100)
            ]);

        var key = Guid.CreateVersion7(DateTimeOffset.UtcNow).ToString();
        var response = await _fixture.Client.PostIdempotentAsync(Root + "/contracts/anticipation", request, key);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var contractList = await response.ReadDataAsync<IReadOnlyList<ContractByContractorAndSituation>>();
        contractList[0].Status.Should().Be(ContractStatusType.PendingRegistration);
        var reference = contractList[0].ExternalReference!;

        var replay = await _fixture.Client.PostIdempotentAsync(Root + "/contracts/anticipation", request, key);
        (await replay.ReadDataAsync<IReadOnlyList<ContractByContractorAndSituation>>())[0].ExternalReference.Should().Be(reference);

        (await _fixture.Client.PostIdempotentAsync(Root + "/contracts/anticipation", request with { RequestedAmount = 200 }, key)).StatusCode.Should().Be(HttpStatusCode.Conflict);

        await _fixture.ProcessAsync("contract");

        (await _fixture.Client.GetAsync(Root + "/contracts?contractorCnpj=" + cnpj + "&situation=ACTIVE")).StatusCode.Should().Be(HttpStatusCode.OK);

        var details = await _fixture.Client.GetDataAsync<ContractByExternalReference>(Root + "/contracts/by-external-reference?contractorCnpj=" + cnpj + "&externalReference=" + reference);
        details.Status.Should().Be(ContractStatusType.Active);

        var updatedAgenda = await _fixture.Client.GetDataAsync<ScheduleQuery>(Root + "/schedules/query-requests/" + id);
        var updatedUnits = updatedAgenda.ScheduleQueryData!.Acquirers![0].PaymentArrangements![0].ReceivableUnits!;
        updatedUnits[0].FreeAmount.Should().Be(9900m);
        updatedUnits[1].FreeAmount.Should().Be(10000m);

        (await _fixture.Client.PostIdempotentAsync(Root + "/contracts/anticipation", request, key)).StatusCode.Should().Be(HttpStatusCode.Created);

        var entry = new BankReconciliationEntry(
            EntryId: Guid.CreateVersion7(DateTimeOffset.UtcNow).ToString(),
            Merchant: cnpj,
            Acquirer: "82413081000116",
            BankAccount: "9999",
            ReferenceDate: new BusinessCalendar(TimeProvider.System, MockEntities.Options()).Today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            Value: 100);

        (await _fixture.Client.PostAsJsonAsync(Root + "/reconciliation/entry", entry, JsonExtensions.Options)).StatusCode.Should().Be(HttpStatusCode.Created);
        (await _fixture.Client.PostAsJsonAsync(Root + "/reconciliation/entry", entry, JsonExtensions.Options)).StatusCode.Should().Be(HttpStatusCode.BadRequest);

        using var scope = _fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SimulatorDbContext>();
        (await db.Deliveries.CountAsync()).Should().Be(3);
        (await db.Entries.CountAsync()).Should().Be(1);

        var delivery = await db.Deliveries.Where(x => x.Kind == "schedule").OrderBy(x => x.NextAttemptAt).FirstAsync();
        var payload = delivery.Payload;
        var eventKey = delivery.Id;
        payload.Should().Contain("dadosConsultaAgenda");

        await scope.ServiceProvider.GetRequiredService<WebhookDispatcher>().DispatchAsync(CancellationToken.None);
        delivery.Attempts.Should().Be(1);
        delivery.Delivered.Should().BeFalse();

        delivery.NextAttemptAt = DateTime.UtcNow.AddSeconds(-1);
        await db.SaveChangesAsync();

        await scope.ServiceProvider.GetRequiredService<WebhookDispatcher>().DispatchAsync(CancellationToken.None);
        delivery.DeadLetter.Should().BeTrue();
        delivery.Id.Should().Be(eventKey);
        delivery.Payload.Should().Be(payload);

        var processor = scope.ServiceProvider.GetRequiredService<OperationProcessor>();
        await processor.RepublishScheduleAsync(Guid.Parse(id), CancellationToken.None);
        (await db.Deliveries.CountAsync(x => x.Kind == "schedule")).Should().Be(3);

        (await _fixture.Client.DeleteAsync(Root + "/merchants/" + cnpj)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await _fixture.Client.DeleteAsync(Root + "/merchants/" + cnpj)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await _fixture.Client.GetAsync(Root + "/merchants/" + cnpj)).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CatalogFiltersWorkWithoutCredentials()
    {
        // Arrange & Act & Assert
        (await _fixture.Client.GetAsync(Root + "/bases-control/acquirers?cnpj=82413081000116")).StatusCode.Should().Be(HttpStatusCode.OK);

        var arrangements = await _fixture.Client.GetDataAsync<IReadOnlyList<PaymentArrangement>>(Root + "/bases-control/payment-arrangements?arrangementCode=VCC");
        arrangements.Should().ContainSingle();

        (await _fixture.Client.GetAsync(Root + "/bases-control/acquirers?cnpj=invalid")).StatusCode.Should().Be(HttpStatusCode.BadRequest);

        using var anonymous = _fixture.Factory.CreateClient();
        (await anonymous.GetAsync(Root + "/bases-control/acquirers")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await anonymous.GetAsync(Root + "/bases-control/acquirers")).StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
