namespace MVFC.Veragi.Simulator.Domain.Tests.Infrastructure;

internal sealed class FixedClock : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => new(2026, 10, 2, 15, 0, 0, TimeSpan.Zero);
}
