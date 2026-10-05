using MVFC.Veragi.Simulator.Domain.Behaviors;
using MediatR;
using OperationResult;

namespace MVFC.Veragi.Simulator.Domain.Handlers.Merchants.DeleteMerchant;

public sealed record DeleteMerchantCommand(string Cnpj) : SimulatedCommand<bool>
{
    public override string Target => "merchant-delete";
    public override string? MerchantCnpj => Cnpj;
}
