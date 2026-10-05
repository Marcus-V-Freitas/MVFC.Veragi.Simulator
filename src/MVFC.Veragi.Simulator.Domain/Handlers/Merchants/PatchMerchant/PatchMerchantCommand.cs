using MVFC.Veragi.Simulator.Shareable.Requests.Merchants;
using MVFC.Veragi.Simulator.Shareable.Responses.Merchants;
using MVFC.Veragi.Simulator.Domain.Behaviors;
using MediatR;
using OperationResult;

namespace MVFC.Veragi.Simulator.Domain.Handlers.Merchants.PatchMerchant;

public sealed record PatchMerchantCommand(
    string Cnpj,
    MerchantPatchRequest Request
) : SimulatedCommand<Merchant>
{
    public override string Target => "merchant-patch";
    public override string? MerchantCnpj => Cnpj;
}
