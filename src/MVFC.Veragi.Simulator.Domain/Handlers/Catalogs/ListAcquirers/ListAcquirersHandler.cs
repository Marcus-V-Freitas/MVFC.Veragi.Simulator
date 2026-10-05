using MVFC.Veragi.Simulator.Domain.Services.Catalogs;
using MVFC.Veragi.Simulator.Shareable.Responses.Catalogs;
using MediatR;
using OperationResult;

namespace MVFC.Veragi.Simulator.Domain.Handlers.Catalogs.ListAcquirers;

public sealed class ListAcquirersHandler(CatalogService catalog) : IRequestHandler<ListAcquirersCommand, Result<IReadOnlyList<Acquirer>>>
{
    private readonly CatalogService _catalog = catalog;

    public Task<Result<IReadOnlyList<Acquirer>>> Handle(ListAcquirersCommand command, CancellationToken cancellationToken) =>
        Task.FromResult(_catalog.GetAcquirers(command.Cnpj));
}
