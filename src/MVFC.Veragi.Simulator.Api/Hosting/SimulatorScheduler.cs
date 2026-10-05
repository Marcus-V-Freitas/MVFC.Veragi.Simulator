using MVFC.Veragi.Simulator.Domain.Handlers.Simulation.ProcessOperations;
using MVFC.Veragi.Simulator.Domain.Handlers.Webhooks.DispatchWebhooks;
using MediatR;
using MVFC.Veragi.Simulator.Shareable.Configuration;
using MVFC.Veragi.Simulator.Shareable.Logging;

namespace MVFC.Veragi.Simulator.Api.Hosting;

public sealed class SimulatorScheduler(
    string kind,
    IServiceScopeFactory scopes,
    SimulatorOptions options,
    ILogger<SimulatorScheduler> logger
) : BackgroundService
{
    private readonly string _kind = kind;
    private readonly IServiceScopeFactory _scopes = scopes;
    private readonly SimulatorOptions _options = options;
    private readonly ILogger<SimulatorScheduler> _logger = logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.SchedulersEnabled)
            return;

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(_options.PollIntervalSeconds));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (!await timer.WaitForNextTickAsync(stoppingToken))
                    break;

                await using var scope = _scopes.CreateAsyncScope();

                if (_kind == "delivery")
                    await scope.ServiceProvider.GetRequiredService<ISender>().Send(new DispatchWebhooksCommand(), stoppingToken);
                else
                    await scope.ServiceProvider.GetRequiredService<ISender>().Send(new ProcessOperationsCommand(_kind, Force: false), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogSchedulerFailed(exception, _kind);
            }
        }
    }
}
