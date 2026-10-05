using MVFC.Veragi.Simulator.Shareable.Enums;

namespace MVFC.Veragi.Simulator.Shareable.Requests.Simulation;

public sealed record SimulationScenarioRequest(
    string? MerchantCnpj = null,
    bool HoldProcessing = default,
    int FailuresRemaining = default,
    int FailureStatusCode = 503,
    List<ScheduleQueryStatusType>? ProcessingOutcomes = null,
    List<ContractStatusType>? ContractStatuses = null
);
