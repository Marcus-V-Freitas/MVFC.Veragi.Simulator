using MVFC.Veragi.Simulator.Shareable.Responses.Catalogs;
using MVFC.Veragi.Simulator.Domain.Behaviors;
using MediatR;
using OperationResult;

namespace MVFC.Veragi.Simulator.Domain.Handlers.Catalogs.ListArrangements;

public sealed record ListArrangementsCommand(string? ArrangementCode) : SimulatedCommand<IReadOnlyList<PaymentArrangement>>
{
    public override string Target => "arrangements-list";
}
