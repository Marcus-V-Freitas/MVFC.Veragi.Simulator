using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MVFC.Veragi.Simulator.Api.Tests.Infrastructure;
using MVFC.Veragi.Simulator.Data;
using MVFC.Veragi.Simulator.Domain.Services.Common;
using MVFC.Veragi.Simulator.Shareable.Enums;
using MVFC.Veragi.Simulator.Shareable.Extensions;
using MVFC.Veragi.Simulator.Shareable.Requests.Contracts;
using MVFC.Veragi.Simulator.Shareable.Requests.Merchants;
using MVFC.Veragi.Simulator.Shareable.Requests.Reconciliation;
using MVFC.Veragi.Simulator.Shareable.Requests.Sales;
using MVFC.Veragi.Simulator.Shareable.Requests.Schedules;
using MVFC.Veragi.Simulator.Shareable.Responses.Contracts;
using MVFC.Veragi.Simulator.Shareable.Responses.Reconciliation;
using MVFC.Veragi.Simulator.Shareable.Responses.Schedules;
using MVFC.Veragi.Simulator.TestHelpers;
using Xunit;

namespace MVFC.Veragi.Simulator.Api.Tests.Reconciliation;

[Collection(SimulatorCollectionDefinition.Name)]
public sealed class ReconciliationFlowTests(SimulatorFixture fixture) : IClassFixture<SimulatorFixture>
{
    private readonly SimulatorFixture _fixture = fixture;
    private const string Root = "/module/card-receivable";
    private const string Cnpj = "44185894000174";

    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(250, 250, 0)]
    [InlineData(500, 500, 0)]
    [InlineData(501, 500, 1)]
    public async Task BankEntryUsesSwaggerPayloadAndPersistsReceivableAllocations(decimal value, decimal allocated, decimal unmatched)
    {
        // Arrange
        var (initialAgendaId, date) = await PrepareAsync();
        await ContractAsync(500, date);
        await _fixture.ProcessAsync("contract");

        var entry = new BankReconciliationEntry(
            EntryId: Guid.CreateVersion7(DateTimeOffset.UtcNow).ToString(),
            ReferenceDate: new BusinessCalendar(TimeProvider.System, MockEntities.Options()).Today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            Merchant: Cnpj,
            Acquirer: "82413081000116",
            BankAccount: "9999",
            Value: value);

        // Act
        var response = await _fixture.Client.PostAsJsonAsync(Root + "/reconciliation/entry", entry, JsonExtensions.Options);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        (await response.Content.ReadAsStringAsync()).Should().BeEmpty();
        (await _fixture.Client.PostAsJsonAsync(Root + "/reconciliation/entry", entry, JsonExtensions.Options)).StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var journal = await _fixture.Client.GetFromJsonAsync<IReadOnlyList<ReconciliationEntryResponse>>(
            "/_simulator/reconciliation/entries?merchantCnpj=" + Cnpj, JsonExtensions.Options);
        journal.Should().NotBeNull();
        journal.Should().ContainSingle();

        var entryResponse = journal![0];
        entryResponse.UnallocatedAmount.Should().Be(unmatched);

        var receivables = entryResponse.Receivables.Deserialize<IReadOnlyList<ReconciledReceivableResponse>>(JsonExtensions.Options)!;
        receivables.Sum(unit => unit.Amount).Should().Be(allocated);

        foreach (var unit in receivables)
        {
            unit.SettlementDate.Should().Be(date);
            unit.HolderCnpj.Should().Be(Cnpj);
        }

        entryResponse.Entry!.AsObject().Should().NotContainKey("externalReference");

        using var scope = _fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SimulatorDbContext>();
        (await db.Entries.SingleAsync()).UnallocatedAmount.Should().Be(unmatched);
        (await db.Operations.SingleAsync(operation => operation.Kind == "contract")).SettlementBankAccountCode.Should().Be("9999");
        (await UnitAsync(initialAgendaId)).FreeAmount.Should().Be(500m);
    }

    [Fact]
    public async Task MultiplePaymentEventsCanReconcileSameInstallmentWithoutDuplicatingItsAmount()
    {
        // Arrange
        var (initialAgendaId, date) = await PrepareAsync();
        await ContractAsync(500, date);
        await _fixture.ProcessAsync("contract");

        var entry = new BankReconciliationEntry(
            EntryId: Guid.CreateVersion7(DateTimeOffset.UtcNow).ToString(),
            ReferenceDate: new BusinessCalendar(TimeProvider.System, MockEntities.Options()).Today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            Merchant: Cnpj,
            Acquirer: "82413081000116",
            BankAccount: "9999",
            Value: 250);

        await _fixture.Client.PostAsJsonAsync(Root + "/reconciliation/entry", entry, JsonExtensions.Options);

        // Act
        var response = await _fixture.Client.PostAsJsonAsync(Root + "/reconciliation/entry", entry with { EntryId = Guid.CreateVersion7(DateTimeOffset.UtcNow).ToString(), Value = 251 }, JsonExtensions.Options);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var journal = (await _fixture.Client.GetFromJsonAsync<IReadOnlyList<ReconciliationEntryResponse>>(
            "/_simulator/reconciliation/entries?merchantCnpj=" + Cnpj, JsonExtensions.Options))!;

        journal.Should().HaveCount(2);

        decimal reconciledAmount = 0m;
        decimal unallocatedAmount = 0m;

        foreach (ReconciliationEntryResponse item in journal)
        {
            unallocatedAmount += item.UnallocatedAmount;

            var receivables = item.Receivables
                .Deserialize<IReadOnlyList<ReconciledReceivableResponse>>(JsonExtensions.Options)!;

            foreach (ReconciledReceivableResponse unit in receivables)
            {
                reconciledAmount += unit.Amount;
            }
        }

        reconciledAmount.Should().Be(500);
        unallocatedAmount.Should().Be(1);
        (await UnitAsync(initialAgendaId)).FreeAmount.Should().Be(500m);
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
}
