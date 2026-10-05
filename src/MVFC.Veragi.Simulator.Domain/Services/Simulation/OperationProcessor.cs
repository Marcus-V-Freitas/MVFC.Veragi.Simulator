using MVFC.Veragi.Simulator.Domain.Services.Schedules;
using MVFC.Veragi.Simulator.Domain.Services.Contracts;
using MVFC.Veragi.Simulator.Domain.Services.Common;
using MVFC.Veragi.Simulator.Shareable.Responses.Contracts;
using MVFC.Veragi.Simulator.Shareable.Responses.Merchants;
using MVFC.Veragi.Simulator.Shareable.Responses.Schedules;
using OperationResult;
using MVFC.Veragi.Simulator.Domain.Entities;
using MVFC.Veragi.Simulator.Domain.Mappings;
using MVFC.Veragi.Simulator.Domain.Ports;
using MVFC.Veragi.Simulator.Shareable.Configuration;
using MVFC.Veragi.Simulator.Shareable.Enums;
using MVFC.Veragi.Simulator.Shareable.Extensions;
using MVFC.Veragi.Simulator.Shareable.Results;

namespace MVFC.Veragi.Simulator.Domain.Services.Simulation;

public sealed class OperationProcessor(
    ISimulatorStore store,
    SimulationGate gate,
    ScheduleGenerator generator,
    TimeProvider clock,
    SimulatorOptions options,
    ScenarioService scenarios,
    ContractRegistrationService registration
)
{
    private readonly ISimulatorStore _store = store;
    private readonly SimulationGate _gate = gate;
    private readonly ScheduleGenerator _generator = generator;
    private readonly TimeProvider _clock = clock;
    private readonly SimulatorOptions _options = options;
    private readonly ScenarioService _scenarios = scenarios;
    private readonly ContractRegistrationService _registration = registration;

    public async Task<Result<int>> ProcessAsync(string kind, bool force, CancellationToken cancellationToken)
    {
        using var lease = await _gate.EnterAsync(cancellationToken);
        var now = _clock.GetUtcNow().UtcDateTime;
        var operations = force ? [.. (await _store.GetOperationsAsync(kind, merchantCnpj: null, cancellationToken)).Where(x => x.Status == ScheduleQueryStatusType.PROCESSING)] : await _store.GetDueOperationsAsync(kind, now, cancellationToken);

        foreach (var operation in operations.OrderBy(x => x.CreatedAt).ThenBy(x => x.Id))
        {
            var merchant = await _store.GetMerchantAsync(operation.MerchantCnpj, cancellationToken);

            if (merchant?.IsDeleted != false)
                operation.Outcome = ScheduleQueryStatusType.ERROR;

            var nextStatus = kind == "schedule" ? await _scenarios.NextScheduleStatusAsync(operation.MerchantCnpj, cancellationToken) : ScheduleQueryStatusType.PROCESSED;
            var contractStatus = kind == "contract" ? await _scenarios.NextContractStatusAsync(operation.MerchantCnpj, cancellationToken) : default;

            if (merchant?.IsDeleted != false)
            {
                nextStatus = ScheduleQueryStatusType.ERROR;
                contractStatus = ContractStatusType.Cancelled;
            }

            if (kind == "contract")
            {
                if (contractStatus is ContractStatusType.PendingEdit or ContractStatusType.PendingRegistration)
                {
                    nextStatus = ScheduleQueryStatusType.PROCESSING;
                }
                else if (contractStatus == ContractStatusType.Cancelled)
                {
                    nextStatus = ScheduleQueryStatusType.ERROR;
                }
                else
                {
                    nextStatus = ScheduleQueryStatusType.PROCESSED;
                }
            }

            operation.Status = nextStatus;
            operation.Outcome = nextStatus;

            if (nextStatus == ScheduleQueryStatusType.PROCESSING)
                operation.DueAt = now.AddSeconds(Math.Max(1, _options.ProcessingDelaySeconds));

            if (nextStatus == ScheduleQueryStatusType.PROCESSING && operation.ProgressNotified == true && (kind == "schedule" || operation.ResultJson.FromJson<ContractByExternalReference>()!.Status == contractStatus))
            {
                await _store.SaveAsync(cancellationToken);
                continue;
            }

            operation.ProgressNotified = nextStatus == ScheduleQueryStatusType.PROCESSING;

            if (kind == "schedule")
                await ProcessScheduleAsync(operation, merchant, now, cancellationToken);
            else
                await ProcessContractAsync(operation, merchant, contractStatus, now, cancellationToken);

            await _store.SaveAsync(cancellationToken);
        }

        return operations.Count;
    }

    private async Task ProcessScheduleAsync(
        ScheduledOperationEntity operation,
        MerchantEntity? merchant,
        DateTime now,
        CancellationToken cancellationToken
    )
    {
        var contracts = await _store.GetOperationsAsync("contract", operation.MerchantCnpj, cancellationToken);
        var data = merchant is null ? operation.ResultJson.FromJson<ScheduleQuery>()!.ScheduleQueryData! : _generator.Generate(operation, merchant.Payload.FromJson<Merchant>()!, contracts, await _store.GetSalesAsync(operation.MerchantCnpj, cancellationToken), now, await _store.GetExternalAnticipationsAsync(operation.MerchantCnpj, cancellationToken));

        if (operation.Outcome == ScheduleQueryStatusType.ERROR)
        {
            data = data with
            {
                Acquirers = null,
                UpdatedAt = now.ToString("O"),
            };
        }

        var result = new ScheduleQuery(Status: operation.Status, Detail: operation.Status == ScheduleQueryStatusType.PROCESSED ? "Schedule query processed" : operation.Status == ScheduleQueryStatusType.PROCESSING ? "Schedule query is processing" : "Schedule query failed", ScheduleQueryData: data);
        operation.ResultJson = result.ToJson();
        await AddDeliveryAsync(operation, _options.ScheduleWebhookSchema == ScheduleWebhookSchema.ContractReceivables ? result.ToContractReceivablesWebhook().ToJson() : result.ToScheduleWebhook().ToJson(), now, cancellationToken);
    }

    private async Task ProcessContractAsync(
        ScheduledOperationEntity operation,
        MerchantEntity? merchant,
        ContractStatusType contractStatus,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var details = await _registration.RegisterAsync(operation, contractStatus, cancellationToken);

        if (details.Status == ContractStatusType.Cancelled)
        {
            operation.Status = ScheduleQueryStatusType.ERROR;
            operation.Outcome = ScheduleQueryStatusType.ERROR;
        }

        operation.ResultJson = details.ToJson();
        await AddDeliveryAsync(operation, operation.ToContractWebhook(now).ToJson(), now, cancellationToken);

        if (details.Status is ContractStatusType.Active or ContractStatusType.Settled && merchant?.IsDeleted == false)
            await UpdateMerchantSchedulesAsync(merchant, now, cancellationToken);
    }

    public async Task<Result<bool>> RepublishScheduleAsync(Guid id, CancellationToken cancellationToken)
    {
        using var lease = await _gate.EnterAsync(cancellationToken);
        var operation = await _store.GetOperationAsync(id, cancellationToken);

        if (operation is null || operation.Kind != "schedule")
            return Failures.Missing("Schedule not found");

        if (operation.Status == ScheduleQueryStatusType.PROCESSING)
            return Failures.Conflict("Schedule is still processing");

        var merchant = await _store.GetMerchantAsync(operation.MerchantCnpj, cancellationToken);

        if (merchant?.IsDeleted != false)
            return Failures.Missing("Merchant not found");

        var now = _clock.GetUtcNow().UtcDateTime;
        await UpdateScheduleAsync(operation, merchant.Payload.FromJson<Merchant>()!, await _store.GetOperationsAsync("contract", merchant.Cnpj, cancellationToken), await _store.GetSalesAsync(merchant.Cnpj, cancellationToken), await _store.GetExternalAnticipationsAsync(merchant.Cnpj, cancellationToken), now, cancellationToken);
        await _store.SaveAsync(cancellationToken);

        return true;
    }

    private async Task UpdateMerchantSchedulesAsync(
        MerchantEntity merchant,
        DateTime now,
        CancellationToken ct
    )
    {
        var schedules = (await _store.GetOperationsAsync("schedule", merchant.Cnpj, ct)).Where(x => x.Status == ScheduleQueryStatusType.PROCESSED).ToArray();

        if (schedules.Length == 0)
            return;

        var contracts = (await _store.GetOperationsAsync("contract", merchant.Cnpj, ct)).Where(x => x.Status != ScheduleQueryStatusType.PROCESSING).ToArray();
        var sales = await _store.GetSalesAsync(merchant.Cnpj, ct);
        var external = await _store.GetExternalAnticipationsAsync(merchant.Cnpj, ct);
        var payload = merchant.Payload.FromJson<Merchant>()!;

        foreach (var schedule in schedules)
            await UpdateScheduleAsync(schedule, payload, contracts, sales, external, now, ct);
    }

    private async Task UpdateScheduleAsync(
        ScheduledOperationEntity operation,
        Merchant merchant,
        IReadOnlyList<ScheduledOperationEntity> contracts,
        IReadOnlyList<SimulatedSaleEntity> sales,
        IReadOnlyList<ExternalAnticipationEntity> external,
        DateTime now,
        CancellationToken ct
    )
    {
        var result = new ScheduleQuery(Status: operation.Status, Detail: "Schedule updated", ScheduleQueryData: _generator.Generate(operation, merchant, contracts, sales, now, external));
        operation.ResultJson = result.ToJson();
        await AddDeliveryAsync(operation, _options.ScheduleWebhookSchema == ScheduleWebhookSchema.ContractReceivables ? result.ToContractReceivablesWebhook().ToJson() : result.ToScheduleWebhook().ToJson(), now, ct);
    }

    private async Task AddDeliveryAsync(
        ScheduledOperationEntity operation,
        string payload,
        DateTime now,
        CancellationToken cancellationToken
    )
    {
        var bytes = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(payload));
        bytes[6] = (byte)((bytes[6] & 0x0f) | 0x80);
        bytes[8] = (byte)((bytes[8] & 0x3f) | 0x80);
        var key = new Guid(bytes.AsSpan(0, 16), bigEndian: true).ToString();

        if (await _store.GetDeliveryByPayloadKeyAsync(key, cancellationToken) is null)
            _store.AddDelivery(new WebhookDeliveryEntity { PayloadKey = key, Kind = operation.Kind, OperationId = operation.Id, MerchantCnpj = operation.MerchantCnpj, ExternalReference = operation.ExternalReference, Payload = payload, NextAttemptAt = now });
    }
}
