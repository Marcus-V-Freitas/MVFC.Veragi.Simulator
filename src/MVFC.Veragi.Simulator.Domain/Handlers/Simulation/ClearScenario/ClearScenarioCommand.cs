using MediatR;
using OperationResult;

namespace MVFC.Veragi.Simulator.Domain.Handlers.Simulation.ClearScenario;

public sealed record ClearScenarioCommand(string Target, string? MerchantCnpj) : IRequest<Result<bool>>;
