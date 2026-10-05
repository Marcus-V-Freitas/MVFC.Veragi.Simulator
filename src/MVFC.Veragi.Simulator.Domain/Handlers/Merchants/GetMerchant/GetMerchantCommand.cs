using MVFC.Veragi.Simulator.Shareable.Responses.Merchants;
using MVFC.Veragi.Simulator.Domain.Behaviors;
using MediatR;
using OperationResult;

namespace MVFC.Veragi.Simulator.Domain.Handlers.Merchants.GetMerchant;

public sealed record GetMerchantCommand(string Cnpj) : SimulatedCommand<Merchant>
{
    public override string Target => "merchant-get";
    public override string? MerchantCnpj => Cnpj;
}
