using MVFC.Veragi.Simulator.Shareable.Responses.Simulation;
using MediatR;
using OperationResult;

namespace MVFC.Veragi.Simulator.Domain.Handlers.Simulation.RunSimulation;

public sealed record RunSimulationCommand() : IRequest<Result<SimulationRunResponse>>;
