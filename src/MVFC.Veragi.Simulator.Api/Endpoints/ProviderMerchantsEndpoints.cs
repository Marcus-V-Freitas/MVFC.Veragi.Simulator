using MVFC.Veragi.Simulator.Domain.Handlers.Merchants.CreateMerchant;
using MVFC.Veragi.Simulator.Domain.Handlers.Merchants.DeleteMerchant;
using MVFC.Veragi.Simulator.Domain.Handlers.Merchants.GetMerchant;
using MVFC.Veragi.Simulator.Domain.Handlers.Merchants.PatchMerchant;
using MVFC.Veragi.Simulator.Shareable.Requests.Merchants;
using MediatR;
using MVFC.Veragi.Simulator.Api.Extensions;

namespace MVFC.Veragi.Simulator.Api.Endpoints;

public static class ProviderMerchantsEndpoints
{
    public static void MapProviderMerchants(this RouteGroupBuilder group)
    {
        group.MapPost("/merchants", async (MerchantCreateRequest request, ISender sender, HttpContext http, CancellationToken ct) =>
            (await sender.Send(new CreateMerchantCommand(request), ct)).ToHttp(http, 201)).WithName("MerchantCreate");

        group.MapGet("/merchants/{cnpj}", async (string cnpj, ISender sender, HttpContext http, CancellationToken ct) =>
            (await sender.Send(new GetMerchantCommand(cnpj), ct)).ToHttp(http)).WithName("MerchantGetByCnpj");

        group.MapPatch("/merchants/{cnpj}", async (string cnpj, MerchantPatchRequest request, ISender sender, HttpContext http, CancellationToken ct) =>
            (await sender.Send(new PatchMerchantCommand(cnpj, request), ct)).ToHttp(http)).WithName("MerchantPatchByCnpj");

        group.MapDelete("/merchants/{cnpj}", async (string cnpj, ISender sender, HttpContext http, CancellationToken ct) =>
            (await sender.Send(new DeleteMerchantCommand(cnpj), ct)).ToHttp(http, 204, noBody: true)).WithName("MerchantDeleteByCnpj");
    }
}
