using MVFC.Veragi.Simulator.Domain.Handlers.Catalogs.ListAcquirers;
using MVFC.Veragi.Simulator.Domain.Handlers.Catalogs.ListArrangements;
using MediatR;
using MVFC.Veragi.Simulator.Api.Extensions;

namespace MVFC.Veragi.Simulator.Api.Endpoints;

public static class ProviderCatalogsEndpoints
{
    public static void MapProviderCatalogs(this RouteGroupBuilder group)
    {
        group.MapGet("/bases-control/acquirers", async (ISender sender, HttpContext http, CancellationToken ct) => (await sender.Send(new ListAcquirersCommand(http.Request.Query.ContainsKey("cnpj") ? http.Request.Query["cnpj"].ToString() : null), ct)).ToHttp(http)).WithName("crBasesControlAcquirerList");
        group.MapGet("/bases-control/payment-arrangements", async (ISender sender, HttpContext http, CancellationToken ct) => (await sender.Send(new ListArrangementsCommand(http.Request.Query.ContainsKey("arrangementCode") ? http.Request.Query["arrangementCode"].ToString() : null), ct)).ToHttp(http)).WithName("crBasesControlPaymentArrangementList");
    }
}
