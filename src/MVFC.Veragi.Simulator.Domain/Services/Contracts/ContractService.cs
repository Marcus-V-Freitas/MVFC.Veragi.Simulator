using MVFC.Veragi.Simulator.Domain.Services.Catalogs;
using MVFC.Veragi.Simulator.Domain.Services.Common;
using MVFC.Veragi.Simulator.Shareable.Requests.Contracts;
using MVFC.Veragi.Simulator.Shareable.Responses.Contracts;
using MVFC.Veragi.Simulator.Shareable.Responses.Merchants;
using MVFC.Veragi.Simulator.Shareable.Responses.Schedules;
using MVFC.Veragi.Simulator.Domain.Validation;
using OperationResult;
using MVFC.Veragi.Simulator.Domain.Entities;
using MVFC.Veragi.Simulator.Domain.Mappings;
using MVFC.Veragi.Simulator.Domain.Ports;
using MVFC.Veragi.Simulator.Shareable.Configuration;
using MVFC.Veragi.Simulator.Shareable.Enums;
using MVFC.Veragi.Simulator.Shareable.Extensions;
using MVFC.Veragi.Simulator.Shareable.Results;

namespace MVFC.Veragi.Simulator.Domain.Services.Contracts;

public sealed class ContractService(
    ISimulatorStore store,
    RequestValidator validator,
    SimulationGate gate,
    ContractRules rules,
    SimulatorOptions options,
    TimeProvider clock,
    ContractAvailabilityService availability,
    ContractBalanceService balances,
    ContractDebtService debt
)
{
    private readonly ISimulatorStore _store = store;
    private readonly RequestValidator _validator = validator;
    private readonly SimulationGate _gate = gate;
    private readonly ContractRules _rules = rules;
    private readonly SimulatorOptions _options = options;
    private readonly TimeProvider _clock = clock;
    private readonly ContractAvailabilityService _availability = availability;
    private readonly ContractBalanceService _balances = balances;
    private readonly ContractDebtService _debt = debt;

    public async Task<Result<IReadOnlyList<ContractByContractorAndSituation>>> CreateAsync(
        ContractAnticipationCreateRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken
    )
    {
        using var lease = await _gate.EnterAsync(cancellationToken);
        var valid = _validator.Validate(request);

        if (!valid.IsSuccess)
            return valid.Exception!;

        if (!Guid.TryParse(idempotencyKey, out var key))
            return Failures.Validation("Idempotency-Key must be a UUID", "Idempotency-Key");

        idempotencyKey = key.ToString();
        var operations = await _store.GetOperationsAsync("contract", merchantCnpj: null, cancellationToken);
        var existing = operations.FirstOrDefault(x => x.IdempotencyKey == idempotencyKey);

        if (existing is not null)
            return existing.RequestHash == request.Fingerprint() ? Result.Success<IReadOnlyList<ContractByContractorAndSituation>>([existing.ToSummary()]) : Failures.Conflict("Idempotency-Key reused with a different payload");

        var merchant = await _store.GetMerchantAsync(request.ContractorCnpj!, cancellationToken);

        if (merchant?.IsDeleted != false)
            return Failures.Missing("Merchant not found");

        var validation = _rules.Validate(request);

        if (!validation.IsSuccess)
            return validation.Exception!;

        var state = merchant.Payload.FromJson<Merchant>()!;

        if (state.CreditConfigurations?.Contains(CreditConfigurationType.OccasionalAnticipation) == false)
            return Failures.Unprocessable("Merchant not enabled for occasional anticipation");

        if (request.SettlementBankAccount is null && state.AnticipationSettlementAccount is null)
            return Failures.Unprocessable("Anticipation settlement account is required");

        var schedules = await _store.GetOperationsAsync("schedule", merchant.Cnpj, cancellationToken);
        var latest = schedules.Where(x => x.Status == ScheduleQueryStatusType.PROCESSED).OrderByDescending(x => x.CreatedAt).FirstOrDefault();

        if (latest is null)
            return Failures.Unprocessable("A processed schedule is required");

        var schedule = latest.ResultJson.FromJson<ScheduleQuery>()!;

        if (!AreGuaranteesInSchedule(request, schedule))
            return Failures.Unprocessable("Guarantee must reference a receivable unit from a processed schedule");

        if (await IsAvailableBalanceInsufficientAsync(request, cancellationToken))
            return new SimulationFailureException(422, "INSUFFICIENT_RECEIVABLE_BALANCE", "Selected receivable units have no available balance");

        return await CreateAndSaveOperationAsync(request, merchant.Cnpj, idempotencyKey, state, cancellationToken);
    }

    private static bool AreGuaranteesInSchedule(
        ContractAnticipationCreateRequest request,
        ScheduleQuery schedule
    )
    {
        foreach (var group in request.Guarantees!.GroupBy(UnitKey))
        {
            var guarantee = group.First();
            var unit = schedule.ScheduleQueryData!.Acquirers?.FirstOrDefault(x => x.Cnpj == guarantee.AcquirerCnpj)?.PaymentArrangements?.FirstOrDefault(x => x.Code == guarantee.PaymentArrangementCode)?.ReceivableUnits?.FirstOrDefault(x => x.HolderCnpj == guarantee.ReceivableUnitHolderCnpj && x.SettlementDate == guarantee.SettlementDate);

            if (unit is null)
                return false;
        }

        return true;
    }

    private async Task<bool> IsAvailableBalanceInsufficientAsync(
        ContractAnticipationCreateRequest request,
        CancellationToken cancellationToken
    )
    {
        if (await _availability.GetModeAsync(cancellationToken) != ContractAvailabilityMode.IMMEDIATE)
            return false;

        var reached = await _balances.GetReachedAsync(request, excludedOperation: null, includePending: true, cancellationToken);
        return reached.Values.Sum() == 0;
    }

    private async Task<Result<IReadOnlyList<ContractByContractorAndSituation>>> CreateAndSaveOperationAsync(
        ContractAnticipationCreateRequest request,
        string merchantCnpj,
        string idempotencyKey,
        Merchant state,
        CancellationToken cancellationToken
    )
    {
        var now = _clock.GetUtcNow().UtcDateTime;
        var operation = request.ToOperation(merchantCnpj, idempotencyKey, now, _options);
        operation.SettlementBankAccountCode = (request.SettlementBankAccount?.Account ?? state.AnticipationSettlementAccount!.Account)!;
        _store.AddOperation(operation);
        await _store.SaveAsync(cancellationToken);

        return Result.Success<IReadOnlyList<ContractByContractorAndSituation>>([operation.ToSummary()]);
    }

    public async Task<Result<IReadOnlyList<ContractByContractorAndSituation>>> ListAsync(
        string cnpj,
        string? situation,
        CancellationToken cancellationToken
    )
    {
        if (!CatalogService.IsCnpj(cnpj))
            return Failures.Validation("Invalid contractor CNPJ", "contractorCnpj");

        if (situation is not null && situation != "ACTIVE")
            return Failures.Validation("Only ACTIVE situation is supported", "situation");

        var operations = await _store.GetOperationsAsync("contract", cnpj, cancellationToken);

        return Result.Success<IReadOnlyList<ContractByContractorAndSituation>>(
            [.. operations.Select(x => x.ToSummary())
                          .Where(x => situation is null || x.Status == ContractStatusType.Active)]);
    }

    public async Task<Result<ContractByExternalReference>> GetAsync(
        string cnpj,
        string externalReference,
        CancellationToken cancellationToken
    )
    {
        if (!CatalogService.IsCnpj(cnpj))
            return Failures.Validation("Invalid contractor CNPJ", "contractorCnpj");

        if (externalReference.Length is < 1 or > 45)
            return Failures.Validation("externalReference must contain 1 to 45 characters", "externalReference");

        var operations = await _store.GetOperationsAsync("contract", cnpj, cancellationToken);
        var operation = operations.FirstOrDefault(x => x.ExternalReference == externalReference);

        if (operation is null)
            return Failures.Missing("Contract not found");

        var registered = operation.ResultJson.FromJson<ContractByExternalReference>()!;
        return await _debt.ProjectAsync(cnpj, externalReference, registered, cancellationToken);
    }

    public static string UnitKey(ContractAnticipationGuaranteeCreate unit) => string.Join('|', unit.ReceivableUnitHolderCnpj, unit.AcquirerCnpj, unit.PaymentArrangementCode, unit.SettlementDate);
}
