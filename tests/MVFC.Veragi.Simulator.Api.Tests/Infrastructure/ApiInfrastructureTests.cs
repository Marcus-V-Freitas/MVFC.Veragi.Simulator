using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace MVFC.Veragi.Simulator.Api.Tests.Infrastructure;

[Collection(SimulatorCollectionDefinition.Name)]
public sealed class ApiInfrastructureTests(SimulatorFixture fixture) : IClassFixture<SimulatorFixture>
{
    private readonly SimulatorFixture _fixture = fixture;

    [Fact]
    public async Task ControlRoutesCanBeDisabledThroughConfiguration()
    {
        // Arrange
        await using var factory = _fixture.Factory.WithWebHostBuilder(builder => builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?> { ["Simulator:EnableControlEndpoints"] = "false" })));
        using var client = factory.CreateClient();

        // Act
        var response = await client.PostAsync("/_simulator/reset", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
