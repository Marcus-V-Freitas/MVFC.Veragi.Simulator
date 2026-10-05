namespace MVFC.Veragi.Simulator.Domain.Services.Common;

internal sealed class SimulationGateLease(SemaphoreSlim semaphore) : IDisposable
{
    private readonly SemaphoreSlim _semaphore = semaphore;
    private bool disposed;

    public void Dispose()
    {
        if (disposed)
            return;

        disposed = true;
        _semaphore.Release();
    }
}
