using MVFC.Veragi.Simulator.Api.Extensions;
using MVFC.Veragi.Simulator.Shareable.Configuration;

namespace MVFC.Veragi.Simulator.Api.Endpoints;

public static class SimulationEndpoints
{
    public static void MapSimulationEndpoints(this WebApplication app)
    {
        var options = app.Services.GetRequiredService<SimulatorOptions>();

        if (!options.EnableControlEndpoints)
            return;

        var group = app.MapGroup("/_simulator");
        group.MapSimulationSales();
        group.MapSimulationWebhooks();
        group.MapSimulationContractAvailability();
        group.MapSimulationProcessing();
        group.MapSimulationScenarios();
        group.MapSimulationEnvironment();
        group.MapSimulationReconciliation();
        app.MapSimulationWebhookReceivers();
    }
}
