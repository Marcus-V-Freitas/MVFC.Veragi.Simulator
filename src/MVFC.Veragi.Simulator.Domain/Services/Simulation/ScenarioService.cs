using MVFC.Veragi.Simulator.Domain.Services.Catalogs;
using MVFC.Veragi.Simulator.Domain.Services.Common;
using MVFC.Veragi.Simulator.Domain.Mappings;
using MVFC.Veragi.Simulator.Shareable.Requests.Simulation;
using MVFC.Veragi.Simulator.Shareable.Responses.Simulation;
using OperationResult;
using MVFC.Veragi.Simulator.Domain.Entities;
using MVFC.Veragi.Simulator.Domain.Ports;
using MVFC.Veragi.Simulator.Shareable.Enums;
using MVFC.Veragi.Simulator.Shareable.Extensions;
using MVFC.Veragi.Simulator.Shareable.Results;

namespace MVFC.Veragi.Simulator.Domain.Services.Simulation;

public sealed class ScenarioService(ISimulatorStore store, SimulationGate gate)
{
    private readonly ISimulatorStore _store = store;
    private readonly SimulationGate _gate = gate;

    public static IReadOnlyList<string> Targets { get; } = ["merchant-create", "merchant-get", "merchant-patch", "merchant-delete", "acquirers-list", "arrangements-list", "sales-generate", "schedule-submit", "schedule-query", "contract-create", "contract-list", "contract-query", "reconciliation", "schedule-process", "contract-process", "schedule-webhook", "contract-webhook"];

    public async Task<Result<SimulationScenarioResponse>> ConfigureAsync(
        string target,
        SimulationScenarioRequest request,
        CancellationToken ct
    )
    {
        using var lease = await _gate.EnterAsync(ct);
        var outcomes = request.ProcessingOutcomes ?? [];
        var statuses = request.ContractStatuses ?? [];

        if (!Targets.Contains(target))
            return Failures.Validation("Unknown target");

        if (request.MerchantCnpj is not null && !CatalogService.IsCnpj(request.MerchantCnpj))
            return Failures.Validation("Invalid merchant CNPJ");

        if (request.FailuresRemaining is < 0 or > 10000 || request.FailureStatusCode is < 400 or > 599)
            return Failures.Validation("Invalid failure count or HTTP status");

        if (outcomes.Count > 100 || statuses.Count > 100)
            return Failures.Validation("Status sequences must contain at most 100 items");

        if (outcomes.Exists(x => !Enum.IsDefined(x)) || statuses.Exists(x => !Enum.IsDefined(x)))
            return Failures.Validation("Unknown processing or contract status");

        if ((outcomes.Count > 0 && target != "schedule-process") || (statuses.Count > 0 && target != "contract-process"))
            return Failures.Validation("Status sequence does not match target");

        if (request.HoldProcessing && target is not "schedule-process" and not "contract-process")
            return Failures.Validation("holdProcessing is only valid for processing targets");

        var id = Key(target, request.MerchantCnpj);
        var scenario = await _store.GetScenarioAsync(id, ct);

        if (scenario is null)
        {
            scenario = new SimulationScenarioEntity
            {
                ScopeKey = id,
                Target = target,
                MerchantCnpj = request.MerchantCnpj,
            };
            _store.AddScenario(scenario);
        }

        scenario.HoldProcessing = request.HoldProcessing;
        scenario.Calls = 0;
        scenario.FailuresRemaining = request.FailuresRemaining;
        scenario.FailureStatusCode = request.FailureStatusCode;
        scenario.ProcessingOutcomesJson = outcomes.ToJson();
        scenario.ContractStatusesJson = statuses.ToJson();
        await _store.SaveAsync(ct);

        return scenario.ToResponse();
    }

    public async Task<Result<bool>> ClearAsync(string target, string? cnpj, CancellationToken ct)
    {
        using var lease = await _gate.EnterAsync(ct);

        if (!Targets.Contains(target) || (cnpj is not null && !CatalogService.IsCnpj(cnpj)))
            return Failures.Validation("Invalid target or merchant CNPJ");

        var scenario = await _store.GetScenarioAsync(Key(target, cnpj), ct);

        if (scenario is not null)
        {
            _store.RemoveScenario(scenario);
            await _store.SaveAsync(ct);
        }

        return true;
    }

    public async Task<int?> TakeFailureAsync(string target, string? cnpj, CancellationToken ct)
    {
        using var lease = await _gate.EnterAsync(ct);
        var scenario = await FindAsync(target, cnpj, ct);

        if (scenario is null)
            return null;

        scenario.Calls++;
        var failure = scenario.FailuresRemaining > 0;

        if (failure)
            scenario.FailuresRemaining--;

        await _store.SaveAsync(ct);

        return failure ? scenario.FailureStatusCode : null;
    }

    public async Task<ScheduleQueryStatusType> NextScheduleStatusAsync(string cnpj, CancellationToken ct)
    {
        var scenario = await FindAsync("schedule-process", cnpj, ct);

        if (scenario is null)
            return ScheduleQueryStatusType.PROCESSED;

        scenario.Calls++;

        if (scenario.FailuresRemaining > 0)
        {
            scenario.FailuresRemaining--;

            return ScheduleQueryStatusType.ERROR;
        }

        if (scenario.HoldProcessing == true)
            return ScheduleQueryStatusType.PROCESSING;

        var sequence = scenario.ProcessingOutcomesJson.FromJson<List<ScheduleQueryStatusType>>()!;
        var status = sequence.Count == 0 ? ScheduleQueryStatusType.PROCESSED : sequence[0];
        scenario.ProcessingOutcomesJson = sequence.Skip(1).ToArray().ToJson();

        return status;
    }

    public async Task<ContractStatusType> NextContractStatusAsync(string cnpj, CancellationToken ct)
    {
        var scenario = await FindAsync("contract-process", cnpj, ct);

        if (scenario is null)
            return ContractStatusType.Active;

        scenario.Calls++;

        if (scenario.FailuresRemaining > 0)
        {
            scenario.FailuresRemaining--;

            return ContractStatusType.Cancelled;
        }

        if (scenario.HoldProcessing == true)
            return ContractStatusType.PendingRegistration;

        var sequence = scenario.ContractStatusesJson.FromJson<List<ContractStatusType>>()!;
        var status = sequence.Count == 0 ? ContractStatusType.Active : sequence[0];
        scenario.ContractStatusesJson = sequence.Skip(1).ToArray().ToJson();

        return status;
    }

    public async Task<IReadOnlyList<SimulationScenarioResponse>> ListAsync(CancellationToken ct) => [.. (await _store.GetScenariosAsync(ct)).Select(scenario => scenario.ToResponse())];

    private async Task<SimulationScenarioEntity?> FindAsync(
        string target,
        string? cnpj,
        CancellationToken ct) =>
        cnpj is null ? await _store.GetScenarioAsync(Key(target, cnpj: null), ct) :
            await _store.GetScenarioAsync(Key(target, cnpj), ct) ??
            await _store.GetScenarioAsync(Key(target, cnpj: null), ct);

    private static string Key(string target, string? cnpj) =>
        target + ":" + (cnpj ?? "*");
}
