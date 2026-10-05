using MVFC.Veragi.Simulator.Domain.Services.Catalogs;
using MVFC.Veragi.Simulator.Shareable.Responses.Catalogs;
using MediatR;
using OperationResult;

namespace MVFC.Veragi.Simulator.Domain.Handlers.Catalogs.ListArrangements;

public sealed class ListArrangementsHandler(CatalogService catalog) : IRequestHandler<ListArrangementsCommand, Result<IReadOnlyList<PaymentArrangement>>>
{
    private readonly CatalogService _catalog = catalog;

    public Task<Result<IReadOnlyList<PaymentArrangement>>> Handle(ListArrangementsCommand command, CancellationToken cancellationToken) =>
        Task.FromResult(_catalog.GetArrangements(command.ArrangementCode));
}
