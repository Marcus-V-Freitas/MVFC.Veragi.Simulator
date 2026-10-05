using MVFC.Veragi.Simulator.Api.Extensions;

namespace MVFC.Veragi.Simulator.Api.Endpoints;

public static class ProviderEndpoints
{
    public static void MapProviderEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/module/card-receivable");
        group.MapProviderMerchants();
        group.MapProviderSchedules();
        group.MapProviderContracts();
        group.MapProviderReconciliation();
        group.MapProviderCatalogs();
    }
}
