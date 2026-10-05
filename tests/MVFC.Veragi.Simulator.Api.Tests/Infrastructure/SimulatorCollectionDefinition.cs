using Xunit;

namespace MVFC.Veragi.Simulator.Api.Tests.Infrastructure;

[CollectionDefinition(SimulatorCollectionDefinition.Name)]
public sealed class SimulatorCollectionDefinition : ICollectionFixture<MongoFixture>
{
    public const string Name = "ApiSimulator";
}
