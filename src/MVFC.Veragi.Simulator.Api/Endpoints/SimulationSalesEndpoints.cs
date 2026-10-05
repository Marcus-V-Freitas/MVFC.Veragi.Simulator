using MVFC.Veragi.Simulator.Domain.Handlers.Sales.GenerateSales;
using MVFC.Veragi.Simulator.Domain.Handlers.Sales.ListSales;
using MVFC.Veragi.Simulator.Shareable.Requests.Sales;
using MediatR;
using MVFC.Veragi.Simulator.Api.Extensions;

namespace MVFC.Veragi.Simulator.Api.Endpoints;

public static class SimulationSalesEndpoints
{
    public static void MapSimulationSales(this RouteGroupBuilder group)
    {
        group.MapPost("/merchants/{cnpj}/sales/generate", async (string cnpj, GenerateSalesRequest request, ISender sender, HttpContext http, CancellationToken ct) => (await sender.Send(new GenerateSalesCommand(cnpj, request, http.Request.Headers["Idempotency-Key"].ToString()), ct)).ToHttp(http, 201));
        group.MapGet("/merchants/{cnpj}/sales", async (string cnpj, ISender sender, HttpContext http, CancellationToken ct) => (await sender.Send(new ListSalesCommand(cnpj), ct)).ToHttp(http));
    }
}
