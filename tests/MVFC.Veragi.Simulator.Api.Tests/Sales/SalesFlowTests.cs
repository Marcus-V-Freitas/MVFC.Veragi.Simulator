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
using MVFC.Veragi.Simulator.Shareable.Requests.Merchants;
using MVFC.Veragi.Simulator.Shareable.Requests.Sales;
using MVFC.Veragi.Simulator.Shareable.Requests.Schedules;
using MVFC.Veragi.Simulator.Shareable.Responses.Sales;
using MVFC.Veragi.Simulator.Shareable.Responses.Schedules;
using Xunit;

namespace MVFC.Veragi.Simulator.Api.Tests.Sales;

[Collection(SimulatorCollectionDefinition.Name)]
public sealed class SalesFlowTests(SimulatorFixture fixture) : IClassFixture<SimulatorFixture>
{
    private readonly SimulatorFixture _fixture = fixture;
    private const string Cnpj = "33185894000174";
    private const string Root = "/module/card-receivable";

    [Fact]
    public async Task SalesPersistIdempotentlyAndPopulatePreviouslyEmptyAgenda()
    {
        // Arrange & Act
        var merchant = new MerchantCreateRequest(
            Cnpj: Cnpj,
            CorporateName: "Sales Test",
            LimitType: LimitType.Transactional,
            ReceivablesScheduleConfig: new ReceivablesScheduleConfig(
                QueryWindow: QueryWindowType.P6M,
                AcquirerCnpjs: ["82413081000116"],
                ArrangementCodes: ["VCC"]));

        // Assert
        (await _fixture.Client.PostAsJsonAsync(Root + "/merchants", merchant, JsonExtensions.Options)).StatusCode.Should().Be(HttpStatusCode.Created);

        var query = await _fixture.Client.PostIdempotentAsync(
            Root + "/schedules/query-requests",
            new ScheduleQueryRequest(MerchantCnpj: Cnpj, QueryType: ScheduleQueryType.STANDARD));
        var requestId = (await query.ReadDataAsync<ScheduleQueryResponse>()).RequestId!;

        await _fixture.ProcessAsync("schedule");

        var empty = await _fixture.Client.GetDataAsync<ScheduleQuery>(Root + "/schedules/query-requests/" + requestId);
        empty.Status.Should().Be(ScheduleQueryStatusType.PROCESSED);
        empty.ScheduleQueryData!.Acquirers.Should().BeEmpty();

        var key = Guid.CreateVersion7(DateTimeOffset.UtcNow).ToString();
        var payload = new GenerateSalesRequest(Days: 2, SalesPerDay: 3, AmountPerSale: 50m);
        var generated = await _fixture.Client.PostIdempotentAsync("/_simulator/merchants/" + Cnpj + "/sales/generate", payload, key);
        generated.StatusCode.Should().Be(HttpStatusCode.Created);

        var batch = await generated.ReadDataAsync<GenerateSalesResponse>();
        batch.SalesCount.Should().Be(6);
        batch.TotalAmount.Should().Be(300m);

        var replay = await _fixture.Client.PostIdempotentAsync("/_simulator/merchants/" + Cnpj + "/sales/generate", payload, key);
        var replayBatch = await replay.ReadDataAsync<GenerateSalesResponse>();
        replayBatch.BatchId.Should().Be(batch.BatchId);

        (await _fixture.Client.PostIdempotentAsync("/_simulator/merchants/" + Cnpj + "/sales/generate", payload with { AmountPerSale = 100m }, key)).StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await _fixture.Client.PostIdempotentAsync("/_simulator/merchants/" + Cnpj + "/sales/generate", payload with { AmountPerSale = 0 })).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await _fixture.Client.PostIdempotentAsync("/_simulator/merchants/" + Cnpj + "/sales/generate", payload with { ArrangementCodes = ["MCC"] })).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await _fixture.Client.PostIdempotentAsync("/_simulator/merchants/" + Cnpj + "/sales/generate", payload with { Days = 365, SalesPerDay = 1000 })).StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var sales = await _fixture.Client.GetDataAsync<IReadOnlyList<SimulatedSaleResponse>>("/_simulator/merchants/" + Cnpj + "/sales");
        sales.Count.Should().Be(6);
        sales.Sum(x => x.Amount).Should().Be(300m);

        (await _fixture.Client.PostAsync("/_simulator/schedules/" + requestId + "/refresh", null)).StatusCode.Should().Be(HttpStatusCode.OK);

        var agendaAfterRefresh = await _fixture.Client.GetDataAsync<ScheduleQuery>(Root + "/schedules/query-requests/" + requestId);
        var units = agendaAfterRefresh.ScheduleQueryData!.Acquirers![0].PaymentArrangements![0].ReceivableUnits!;
        units.Count.Should().Be(2);
        units.Should().AllSatisfy(unit => unit.TotalAmount.Should().Be(150m));

        (await _fixture.Client.PostIdempotentAsync("/_simulator/merchants/" + Cnpj + "/sales/generate", payload with { Days = 1, SalesPerDay = 1, AmountPerSale = 100m })).StatusCode.Should().Be(HttpStatusCode.Created);
        await _fixture.Client.PostAsync("/_simulator/schedules/" + requestId + "/refresh", null);

        var agendaSecondRefresh = await _fixture.Client.GetDataAsync<ScheduleQuery>(Root + "/schedules/query-requests/" + requestId);
        units = agendaSecondRefresh.ScheduleQueryData!.Acquirers![0].PaymentArrangements![0].ReceivableUnits!;
        units.Sum(x => x.TotalAmount).Should().Be(400m);

        using var persistedScope = _fixture.Factory.Services.CreateScope();
        var db = persistedScope.ServiceProvider.GetRequiredService<SimulatorDbContext>();
        (await db.Sales.CountAsync(x => x.MerchantCnpj == Cnpj)).Should().Be(7);
        (await db.SalesBatches.CountAsync(x => x.MerchantCnpj == Cnpj)).Should().Be(2);
        (await db.Deliveries.CountAsync(x => x.MerchantCnpj == Cnpj)).Should().Be(3);
    }

    [Fact]
    public async Task ExplicitSalesPersistDifferentInstallmentsWithoutSchedulingEventsAndRejectWholeInvalidBatch()
    {
        // Arrange
        const string cnpj = "43185894000174";

        // Act
        var merchant = new MerchantCreateRequest(
            Cnpj: cnpj,
            CorporateName: "Installment Test",
            LimitType: LimitType.Transactional,
            ReceivablesScheduleConfig: new ReceivablesScheduleConfig(
                QueryWindow: QueryWindowType.P6M,
                AcquirerCnpjs: ["01027058000191", "82413081000116"],
                ArrangementCodes: ["VCC", "MCC"]));

        // Assert
        (await _fixture.Client.PostAsJsonAsync(Root + "/merchants", merchant, JsonExtensions.Options)).StatusCode.Should().Be(HttpStatusCode.Created);
        var exactDate = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo")).DateTime).AddDays(30).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        var payload = new GenerateSalesRequest(
            Sales:
            [
                new()
                {
                    ExternalId = "sale-1",
                    AcquirerCnpj = "01027058000191",
                    PaymentArrangementCode = "VCC",
                    Installments =
                    [
                        new() { Amount = 123.45m, SettlementBusinessDays = 5 },
                        new() { Amount = 76.55m, SettlementBusinessDays = 8 }
                    ]
                },
                new()
                {
                    ExternalId = "sale-2",
                    AcquirerCnpj = "82413081000116",
                    PaymentArrangementCode = "MCC",
                    Installments =
                    [
                        new() { Amount = 150m, SettlementDate = exactDate }
                    ]
                }
            ]);

        var url = "/_simulator/merchants/" + cnpj + "/sales/generate";
        var key = Guid.CreateVersion7(DateTimeOffset.UtcNow).ToString();
        var result = await _fixture.Client.PostIdempotentAsync(url, payload, key);
        result.StatusCode.Should().Be(HttpStatusCode.Created);

        var data = await result.ReadDataAsync<GenerateSalesResponse>();
        data.SalesCount.Should().Be(2);
        data.InstallmentCount.Should().Be(3);
        data.TotalAmount.Should().Be(350m);

        var replay = await _fixture.Client.PostIdempotentAsync(url, payload, key);
        var replayData = await replay.ReadDataAsync<GenerateSalesResponse>();
        replayData.BatchId.Should().Be(data.BatchId);

        var sales = await _fixture.Client.GetDataAsync<IReadOnlyList<SimulatedSaleResponse>>("/_simulator/merchants/" + cnpj + "/sales");
        sales.Count.Should().Be(3);

        var firstSale = sales.Where(x => x.ExternalId == "sale-1").ToArray();
        firstSale.Length.Should().Be(2);
        firstSale[1].SaleId.Should().Be(firstSale[0].SaleId);
        sales.Should().ContainSingle(x => x.SettlementDate == exactDate && x.Amount == 150m);

        var invalid = payload with
        {
            Sales =
            [
                payload.Sales![0],
                payload.Sales[1] with
                {
                    Installments = [new() { Amount = 0m, SettlementBusinessDays = 6 }]
                }
            ]
        };

        (await _fixture.Client.PostIdempotentAsync(url, invalid)).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await _fixture.Client.PostIdempotentAsync(url, payload with { Sales = [payload.Sales[0] with { AcquirerCnpj = "16501555000157" }] })).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await _fixture.Client.PostIdempotentAsync(url, payload with { Sales = [payload.Sales[0] with { Installments = [new() { Amount = 10m, SettlementBusinessDays = 5, SettlementDate = "2030-01-01" }] }] })).StatusCode.Should().Be(HttpStatusCode.BadRequest);

        using var scope = _fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SimulatorDbContext>();
        (await db.Sales.CountAsync(x => x.MerchantCnpj == cnpj)).Should().Be(3);
        (await db.Operations.CountAsync(x => x.MerchantCnpj == cnpj)).Should().Be(0);
        (await db.Deliveries.CountAsync(x => x.MerchantCnpj == cnpj)).Should().Be(0);
    }
}
