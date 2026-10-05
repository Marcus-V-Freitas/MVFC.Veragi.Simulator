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
using MVFC.Veragi.Simulator.Shareable.Responses.Contracts;
using MVFC.Veragi.Simulator.Shareable.Responses.Schedules;
using MVFC.Veragi.Simulator.TestHelpers;
using Xunit;

namespace MVFC.Veragi.Simulator.Api.Tests.Contracts;

[Collection(SimulatorCollectionDefinition.Name)]
public sealed class ContractIdentifierFlowTests(SimulatorFixture fixture) : IClassFixture<SimulatorFixture>
{
    private readonly SimulatorFixture _fixture = fixture;
    private const string Root = "/module/card-receivable";

    [Theory]
    [InlineData(null)]
    [InlineData("CLIENT-123")]
    [InlineData("HTTP-1791216610601-TOTAL")]
    public async Task IdentifierSurvivesProcessingRestartAndIdempotentRetry(string? provided)
    {
        // Arrange
        await _fixture.Client.PostAsync("/_simulator/reset", null);
        await _fixture.Client.PostAsJsonAsync(Root + "/merchants", MockEntities.MerchantRequest(), JsonExtensions.Options);
        await _fixture.Client.PostIdempotentAsync("/_simulator/merchants/" + MockEntities.MerchantCnpj + "/sales/generate", new GenerateSalesRequest(Days: 1, SalesPerDay: 1, AmountPerSale: 1000));
        var query = await _fixture.Client.PostIdempotentAsync(Root + "/schedules/query-requests", new ScheduleQueryRequest(MockEntities.MerchantCnpj, ScheduleQueryType.STANDARD));
        var queryId = (await query.ReadDataAsync<ScheduleQueryResponse>()).RequestId;
        await _fixture.ProcessAsync("schedule");
        var agenda = await _fixture.Client.GetDataAsync<ScheduleQuery>(Root + "/schedules/query-requests/" + queryId);
        var date = DateOnly.ParseExact(agenda.ScheduleQueryData!.Acquirers![0].PaymentArrangements![0].ReceivableUnits![0].SettlementDate!, "yyyy-MM-dd", CultureInfo.InvariantCulture);
        var request = MockEntities.Contract(new BusinessCalendar(TimeProvider.System, MockEntities.Options()).Today, date, 100) with { FinancierContractId = provided };
        var key = Guid.CreateVersion7(DateTimeOffset.UtcNow).ToString();
        var created = await _fixture.Client.PostIdempotentAsync(Root + "/contracts/anticipation", request, key);
        var initial = (await created.ReadDataAsync<IReadOnlyList<ContractByContractorAndSituation>>())[0];

        // Act
        await _fixture.ProcessAsync("contract");
        await using var restarted = new SimulatorWebApplicationFactory(_fixture.ConnectionString, _fixture.DatabaseName);
        using var client = restarted.CreateClient();
        var details = await client.GetDataAsync<ContractByExternalReference>(Root + "/contracts/by-external-reference?contractorCnpj=" + MockEntities.MerchantCnpj + "&externalReference=" + initial.ExternalReference);
        var retry = await client.PostIdempotentAsync(Root + "/contracts/anticipation", request, key);
        var repeated = (await retry.ReadDataAsync<IReadOnlyList<ContractByContractorAndSituation>>())[0];
        var listed = await client.GetDataAsync<IReadOnlyList<ContractByContractorAndSituation>>(Root + "/contracts?contractorCnpj=" + MockEntities.MerchantCnpj);

        // Assert
        created.StatusCode.Should().Be(HttpStatusCode.Created);
        retry.StatusCode.Should().Be(HttpStatusCode.Created);
        if (provided is null)
            initial.FinancierContractId.Should().Be(initial.ExternalReference);
        else
            initial.FinancierContractId.Should().Be(provided);

        details.FinancierContractId.Should().Be(initial.FinancierContractId);
        initial.ExternalReference.Should().MatchRegex("^CON/[0-9]{8}/[0-9]{14}/[0-9]{6}/[0-9]{6}$");
        initial.ExternalReference.Should().HaveLength(41);
        repeated.FinancierContractId.Should().Be(initial.FinancierContractId);
        repeated.ExternalReference.Should().Be(initial.ExternalReference);
        listed.Should().ContainSingle();
        listed[0].FinancierContractId.Should().Be(initial.FinancierContractId);
        listed[0].ExternalReference.Should().Be(initial.ExternalReference);
        details.Status.Should().Be(ContractStatusType.Active);
    }
}
