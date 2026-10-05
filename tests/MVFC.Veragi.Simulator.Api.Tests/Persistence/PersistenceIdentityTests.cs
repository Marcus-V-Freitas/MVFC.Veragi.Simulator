using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MVFC.Veragi.Simulator.Api.Tests.Infrastructure;
using MVFC.Veragi.Simulator.Data;
using MVFC.Veragi.Simulator.Shareable.Extensions;
using MVFC.Veragi.Simulator.Shareable.Requests.Sales;
using MVFC.Veragi.Simulator.Shareable.Requests.Simulation;
using MVFC.Veragi.Simulator.Shareable.Requests.Webhooks;
using MVFC.Veragi.Simulator.TestHelpers;
using Xunit;

namespace MVFC.Veragi.Simulator.Api.Tests.Persistence;

[Collection(SimulatorCollectionDefinition.Name)]
public sealed class PersistenceIdentityTests(SimulatorFixture fixture) : IClassFixture<SimulatorFixture>
{
    private readonly SimulatorFixture _fixture = fixture;

    [Fact]
    public async Task AllEntityIdsAreStoredAsUuidVersionSevenAndBusinessKeysRemainQueryable()
    {
        // Arrange
        await _fixture.Client.PostAsync("/_simulator/reset", null);
        var request = MockEntities.MerchantRequest();

        // Act
        var created = await _fixture.Client.PostAsJsonAsync("/module/card-receivable/merchants", request, JsonExtensions.Options);

        // Assert
        created.StatusCode.Should().Be(HttpStatusCode.Created);
        var saleRequest = new HttpRequestMessage(HttpMethod.Post, $"/_simulator/merchants/{request.Cnpj}/sales/generate")
        {
            Content = JsonContent.Create(new GenerateSalesRequest(Days: 1, SalesPerDay: 1), options: JsonExtensions.Options)
        };
        var businessKey = Guid.CreateVersion7(DateTimeOffset.UtcNow);
        saleRequest.Headers.Add("Idempotency-Key", businessKey.ToString());
        using (saleRequest)
            (await _fixture.Client.SendAsync(saleRequest)).StatusCode.Should().Be(HttpStatusCode.Created);

        await _fixture.Client.PutAsJsonAsync("/_simulator/scenarios/merchant-get", new SimulationScenarioRequest(MerchantCnpj: request.Cnpj, FailuresRemaining: 0));
        await _fixture.Client.PutAsJsonAsync("/_simulator/webhooks", new WebhookDestinationsRequest("http://localhost:5090/schedule", "http://localhost:5090/contract"));
        using var scope = _fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SimulatorDbContext>();
        var merchant = await db.Merchants.SingleAsync();
        var sale = await db.Sales.SingleAsync();
        var batch = await db.SalesBatches.SingleAsync();
        var scenario = await db.Scenarios.SingleAsync();
        var routing = await db.WebhookRouting.SingleAsync();
        new[]
        {
            merchant.Id,
            sale.Id,
            batch.Id,
            scenario.Id,
            routing.Id
        }.Should().OnlyContain(x => x.Version == 7);
        batch.Id.Should().NotBe(businessKey);
        batch.BatchKey.Should().Be(request.Cnpj + ":" + businessKey);
        merchant.Cnpj.Should().Be(request.Cnpj);
        (await _fixture.Client.GetAsync("/module/card-receivable/merchants/" + request.Cnpj)).StatusCode.Should().Be(HttpStatusCode.OK);
        await _fixture.Client.PostAsync("/_simulator/reset", null);
    }
}
