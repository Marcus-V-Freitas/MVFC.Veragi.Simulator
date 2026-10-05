namespace MVFC.Veragi.Simulator.Domain.Services.Webhooks;

public sealed class DeliveryGate : IDisposable
{
    private readonly SemaphoreSlim semaphore = new(1, 1);

    public Task EnterAsync(CancellationToken cancellationToken) => semaphore.WaitAsync(cancellationToken);

    public void Exit() => semaphore.Release();

    public void Dispose() => semaphore.Dispose();
}
