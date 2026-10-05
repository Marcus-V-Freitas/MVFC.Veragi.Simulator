using MVFC.Veragi.Simulator.Shareable.Responses.Simulation;
using MediatR;
using OperationResult;

namespace MVFC.Veragi.Simulator.Domain.Handlers.Simulation.ResetEnvironment;

public sealed record ResetEnvironmentCommand() : IRequest<Result<EnvironmentResetResponse>>;
