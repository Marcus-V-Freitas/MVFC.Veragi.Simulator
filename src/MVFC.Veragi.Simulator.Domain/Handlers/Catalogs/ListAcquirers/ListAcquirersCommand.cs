using MVFC.Veragi.Simulator.Shareable.Responses.Catalogs;
using MVFC.Veragi.Simulator.Domain.Behaviors;
using MediatR;
using OperationResult;

namespace MVFC.Veragi.Simulator.Domain.Handlers.Catalogs.ListAcquirers;

public sealed record ListAcquirersCommand(string? Cnpj) : SimulatedCommand<IReadOnlyList<Acquirer>>
{
    public override string Target => "acquirers-list";
}
