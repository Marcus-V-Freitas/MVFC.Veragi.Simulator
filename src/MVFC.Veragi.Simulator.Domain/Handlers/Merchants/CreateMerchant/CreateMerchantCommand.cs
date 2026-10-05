using MVFC.Veragi.Simulator.Shareable.Requests.Merchants;
using MVFC.Veragi.Simulator.Shareable.Responses.Merchants;
using MVFC.Veragi.Simulator.Domain.Behaviors;
using MediatR;
using OperationResult;

namespace MVFC.Veragi.Simulator.Domain.Handlers.Merchants.CreateMerchant;

public sealed record CreateMerchantCommand(MerchantCreateRequest Request) : SimulatedCommand<Merchant>
{
    public override string Target => "merchant-create";
    public override string? MerchantCnpj => Request.Cnpj;
}
