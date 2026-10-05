using Xunit;

namespace MVFC.Veragi.Simulator.Data.Tests.Infrastructure;

[CollectionDefinition(DataCollectionDefinition.Name)]
public sealed class DataCollectionDefinition : ICollectionFixture<MongoFixture>
{
    public const string Name = "DataSimulator";
}
