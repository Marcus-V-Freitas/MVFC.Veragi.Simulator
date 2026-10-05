using MVFC.Veragi.Simulator.Shareable.Enums;

namespace MVFC.Veragi.Simulator.Shareable.Responses.Simulation;

public sealed record SimulationScenarioResponse(
    string Target,
    string? MerchantCnpj,
    int Calls,
    int FailuresRemaining,
    int FailureStatusCode,
    IReadOnlyList<ScheduleQueryStatusType> ProcessingOutcomes,
    IReadOnlyList<ContractStatusType> ContractStatuses,
    bool HoldProcessing
);
