using MVFC.Veragi.Simulator.Shareable.Responses.Sales;
using MediatR;
using OperationResult;

namespace MVFC.Veragi.Simulator.Domain.Handlers.Sales.ListSales;

public sealed record ListSalesCommand(string Cnpj) : IRequest<Result<IReadOnlyList<SimulatedSaleResponse>>>;
