using MVFC.Veragi.Simulator.Domain.Handlers.Contracts.CreateAnticipation;
using MVFC.Veragi.Simulator.Domain.Handlers.Contracts.GetContract;
using MVFC.Veragi.Simulator.Domain.Handlers.Contracts.ListContracts;
using MVFC.Veragi.Simulator.Shareable.Requests.Contracts;
using MediatR;
using MVFC.Veragi.Simulator.Api.Extensions;

namespace MVFC.Veragi.Simulator.Api.Endpoints;

public static class ProviderContractsEndpoints
{
    public static void MapProviderContracts(this RouteGroupBuilder group)
    {
        group.MapPost("/contracts/anticipation", async (ContractAnticipationCreateRequest request, ISender sender, HttpContext http, CancellationToken ct) => (await sender.Send(new CreateAnticipationCommand(request, http.Request.Headers["Idempotency-Key"].ToString()), ct)).ToHttp(http, 201)).WithName("crContractAnticipationCreate");
        group.MapGet("/contracts", async (ISender sender, HttpContext http, CancellationToken ct) => (await sender.Send(new ListContractsCommand(http.Request.Query["contractorCnpj"].ToString(), http.Request.Query.ContainsKey("situation") ? http.Request.Query["situation"].ToString() : null), ct)).ToHttp(http)).WithName("crContractListByContractorAndSituation");
        group.MapGet("/contracts/by-external-reference", async (ISender sender, HttpContext http, CancellationToken ct) => (await sender.Send(new GetContractCommand(http.Request.Query["contractorCnpj"].ToString(), http.Request.Query["externalReference"].ToString()), ct)).ToHttp(http)).WithName("crContractGetByExternalReferenceAndContractorCnpj");
    }
}
