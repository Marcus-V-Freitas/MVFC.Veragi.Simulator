using MVFC.Veragi.Simulator.Domain.Services.Sales;
using MVFC.Veragi.Simulator.Shareable.Responses.Sales;
using MediatR;
using OperationResult;

namespace MVFC.Veragi.Simulator.Domain.Handlers.Sales.ListSales;

public sealed class ListSalesHandler(SalesService sales) : IRequestHandler<ListSalesCommand, Result<IReadOnlyList<SimulatedSaleResponse>>>
{
    private readonly SalesService _sales = sales;

    public Task<Result<IReadOnlyList<SimulatedSaleResponse>>> Handle(ListSalesCommand command, CancellationToken cancellationToken) =>
        _sales.ListAsync(command.Cnpj, cancellationToken);
}
