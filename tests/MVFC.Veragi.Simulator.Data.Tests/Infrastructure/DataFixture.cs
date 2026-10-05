using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using MVFC.Veragi.Simulator.Data;
using MVFC.Veragi.Simulator.Domain.Ports;
using Xunit;

namespace MVFC.Veragi.Simulator.Data.Tests.Infrastructure;

public sealed class DataFixture(MongoFixture mongoFixture) : IAsyncLifetime
{
    private readonly MongoFixture _mongoFixture = mongoFixture;

    public string ConnectionString => _mongoFixture.ConnectionString;

    public string DatabaseName { get; } = "veragi_data_tests_" + Guid.CreateVersion7(DateTimeOffset.UtcNow).ToString("N");

    public IMongoDatabase Database { get; private set; } = null!;

    public SimulatorDbContext DbContext { get; private set; } = null!;

    public ISimulatorStore Store { get; private set; } = null!;

    public Task InitializeAsync()
    {
        var client = new MongoClient(ConnectionString);
        Database = client.GetDatabase(DatabaseName);
        var options = new DbContextOptionsBuilder<SimulatorDbContext>()
            .UseMongoDB(client, DatabaseName)
            .Options;
        DbContext = new SimulatorDbContext(options);
        Store = new MongoSimulatorStore(DbContext, Database);

        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await DbContext.DisposeAsync();
        await new MongoClient(ConnectionString).DropDatabaseAsync(DatabaseName);
    }
}
