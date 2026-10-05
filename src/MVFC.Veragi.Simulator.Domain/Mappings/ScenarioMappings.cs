using MVFC.Veragi.Simulator.Domain.Entities;
using MVFC.Veragi.Simulator.Shareable.Enums;
using MVFC.Veragi.Simulator.Shareable.Extensions;
using MVFC.Veragi.Simulator.Shareable.Responses.Simulation;

namespace MVFC.Veragi.Simulator.Domain.Mappings;

public static class ScenarioMappings
{
    public static SimulationScenarioResponse ToResponse(this SimulationScenarioEntity scenario) =>
        new(scenario.Target, scenario.MerchantCnpj, scenario.Calls, scenario.FailuresRemaining, scenario.FailureStatusCode, scenario.ProcessingOutcomesJson.FromJson<List<ScheduleQueryStatusType>>()!, scenario.ContractStatusesJson.FromJson<List<ContractStatusType>>()!, scenario.HoldProcessing ?? false);
}
