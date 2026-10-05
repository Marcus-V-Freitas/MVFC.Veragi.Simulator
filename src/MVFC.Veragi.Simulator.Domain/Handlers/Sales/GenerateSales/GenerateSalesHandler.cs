using MVFC.Veragi.Simulator.Domain.Services.Sales;
using MVFC.Veragi.Simulator.Shareable.Responses.Sales;
using MediatR;
using OperationResult;

namespace MVFC.Veragi.Simulator.Domain.Handlers.Sales.GenerateSales;

public sealed class GenerateSalesHandler(SalesService sales) : IRequestHandler<GenerateSalesCommand, Result<GenerateSalesResponse>>
{
    private readonly SalesService _sales = sales;

    public Task<Result<GenerateSalesResponse>> Handle(GenerateSalesCommand command, CancellationToken cancellationToken) =>
        _sales.GenerateAsync(command.Cnpj, command.Request, command.IdempotencyKey, cancellationToken);
}
