using System.Net;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.EntityFrameworkCore.Extensions;
using MVFC.Veragi.Simulator.Api.Tests.Infrastructure;
using MVFC.Veragi.Simulator.Data;
using MVFC.Veragi.Simulator.Shareable.Responses.Simulation;
using Xunit;

namespace MVFC.Veragi.Simulator.Api.Tests.Simulation;

[Collection(SimulatorCollectionDefinition.Name)]
public sealed class ResetCompatibilityTests(SimulatorFixture fixture) : IClassFixture<SimulatorFixture>
{
    private readonly SimulatorFixture _fixture = fixture;

    [Fact]
    public async Task ResetDeletesOldAndIncompleteDocumentsWithoutMaterializingAndPreservesUnmappedCollections()
    {
        // Arrange
        using var scope = _fixture.Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<SimulatorDbContext>();
        var database = scope.ServiceProvider.GetRequiredService<IMongoDatabase>();
        var names = context.Model.GetCollectionNames().ToArray();

        foreach (var name in names)
            await database.GetCollection<BsonDocument>(name).InsertOneAsync(new BsonDocument { { "_id", "legacy-" + name }, { "oldField", true } });

        await database.GetCollection<BsonDocument>("unrelated_collection").InsertOneAsync(new BsonDocument("_id", "keep-me"));

        // Act
        var response = await _fixture.Client.PostAsync("/_simulator/reset", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.ReadDataAsync<EnvironmentResetResponse>();
        result.RemovedDocuments.Should().Be(names.Length);

        foreach (var name in names)
            (await database.GetCollection<BsonDocument>(name).CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty)).Should().Be(0);

        (await database.GetCollection<BsonDocument>("unrelated_collection").CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty)).Should().Be(1);

        response = await _fixture.Client.PostAsync("/_simulator/reset", null);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result = await response.ReadDataAsync<EnvironmentResetResponse>();
        result.RemovedDocuments.Should().Be(0);
        (await _fixture.Client.GetAsync("/module/card-receivable/bases-control/acquirers")).StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
