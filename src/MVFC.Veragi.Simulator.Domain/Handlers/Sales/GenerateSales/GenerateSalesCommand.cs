using MVFC.Veragi.Simulator.Shareable.Requests.Sales;
using MVFC.Veragi.Simulator.Shareable.Responses.Sales;
using MVFC.Veragi.Simulator.Domain.Behaviors;
using MediatR;
using OperationResult;

namespace MVFC.Veragi.Simulator.Domain.Handlers.Sales.GenerateSales;

public sealed record GenerateSalesCommand(
    string Cnpj,
    GenerateSalesRequest Request,
    string IdempotencyKey
) : SimulatedCommand<GenerateSalesResponse>
{
    public override string Target => "sales-generate";
    public override string? MerchantCnpj => Cnpj;
}
