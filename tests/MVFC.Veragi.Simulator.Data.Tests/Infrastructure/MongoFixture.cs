using MongoDB.Bson;
using MongoDB.Driver;
using Testcontainers.MongoDb;
using Xunit;

namespace MVFC.Veragi.Simulator.Data.Tests.Infrastructure;

public sealed class MongoFixture : IAsyncLifetime
{
    private MongoDbContainer? container;

    public string ConnectionString { get; private set; } = "";

    public async Task InitializeAsync()
    {
        ConnectionString = Environment.GetEnvironmentVariable("SIMULATOR_TEST_MONGO") ?? "";

        if (ConnectionString.Length == 0)
        {
            container = new MongoDbBuilder("mongo:8.0").WithReplicaSet("rs0").Build();
            await container.StartAsync();
            ConnectionString = container.GetConnectionString();
        }

        var mongo = new MongoClient(ConnectionString);
        for (var attempt = 0; attempt < 100; attempt++)
        {
            var hello = await mongo.GetDatabase("admin").RunCommandAsync<BsonDocument>(new BsonDocument("hello", 1));

            if (hello.GetValue("isWritablePrimary", false).AsBoolean)
                break;

            await Task.Delay(100);
        }
    }

    public async Task DisposeAsync()
    {
        if (container is not null)
            await container.DisposeAsync();
    }
}
