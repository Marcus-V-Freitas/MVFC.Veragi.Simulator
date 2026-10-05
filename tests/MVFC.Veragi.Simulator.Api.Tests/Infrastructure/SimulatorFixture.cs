using Microsoft.AspNetCore.Mvc.Testing;
using MongoDB.Driver;
using MVFC.Veragi.Simulator.Api;
using Xunit;

namespace MVFC.Veragi.Simulator.Api.Tests.Infrastructure;

public sealed class SimulatorFixture(MongoFixture mongoFixture) : IAsyncLifetime
{
    private readonly MongoFixture _mongoFixture = mongoFixture;

    public WebApplicationFactory<IApiEntryPoint> Factory { get; private set; } = null!;

    public HttpClient Client { get; private set; } = null!;

    public string ConnectionString => _mongoFixture.ConnectionString;

    public string DatabaseName { get; } = "veragi_api_tests_" + Guid.CreateVersion7(DateTimeOffset.UtcNow).ToString("N");

    public Task InitializeAsync()
    {
        Factory = new SimulatorWebApplicationFactory(ConnectionString, DatabaseName);
        Client = Factory.CreateClient();

        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        Client.Dispose();
        await Factory.DisposeAsync();
        await new MongoClient(ConnectionString).DropDatabaseAsync(DatabaseName);
    }
}
