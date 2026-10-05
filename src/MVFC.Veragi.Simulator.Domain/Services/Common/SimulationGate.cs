namespace MVFC.Veragi.Simulator.Domain.Services.Common;

public sealed class SimulationGate : IDisposable
{
    private readonly SemaphoreSlim semaphore = new(1, 1);

    public async Task<IDisposable> EnterAsync(CancellationToken cancellationToken)
    {
        await semaphore.WaitAsync(cancellationToken);

        return new SimulationGateLease(semaphore);
    }

    public void Dispose() => semaphore.Dispose();
}
