using OperationResult;
using System.Globalization;
using MVFC.Veragi.Simulator.Domain.Entities;
using MVFC.Veragi.Simulator.Domain.Ports;
using MVFC.Veragi.Simulator.Domain.Services.Catalogs;
using MVFC.Veragi.Simulator.Domain.Services.Common;
using MVFC.Veragi.Simulator.Domain.Validation;
using MVFC.Veragi.Simulator.Shareable.Enums;
using MVFC.Veragi.Simulator.Shareable.Extensions;
using MVFC.Veragi.Simulator.Shareable.Requests.Reconciliation;
using MVFC.Veragi.Simulator.Shareable.Responses.Contracts;
using MVFC.Veragi.Simulator.Shareable.Results;

namespace MVFC.Veragi.Simulator.Domain.Services.Reconciliation;

public sealed class ReconciliationService(
    ISimulatorStore store,
    RequestValidator validator,
    SimulationGate gate,
    BusinessCalendar calendar,
    ReconciliationAllocationService allocations)
{
    private readonly ISimulatorStore _store = store;
    private readonly RequestValidator _validator = validator;
    private readonly SimulationGate _gate = gate;
    private readonly BusinessCalendar _calendar = calendar;
    private readonly ReconciliationAllocationService _allocations = allocations;

    public async Task<Result<bool>> CreateAsync(
        BankReconciliationEntry request,
        CancellationToken cancellationToken)
    {
        using var lease = await _gate.EnterAsync(cancellationToken);
        var valid = _validator.Validate(request);

        if (!valid.IsSuccess)
            return valid.Exception!;

        if (!CatalogService.IsCnpj(request.Merchant!) || !CatalogService.IsCnpj(request.Acquirer!))
            return Failures.Validation("Invalid merchant or acquirer CNPJ");

        if (request.ReferenceDate != _calendar.Today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))
            return Failures.Validation("referenceDate must equal current date", "referenceDate");

        if (request.Value < 0)
            return Failures.Validation("value must be nonnegative", "value");

        if (string.IsNullOrWhiteSpace(request.BankAccount))
            return Failures.Validation("bankAccount required", "bankAccount");

        var id = Guid.Parse(request.EntryId!);

        if (await _store.GetEntryAsync(id, cancellationToken) is not null)
            return Failures.Validation("entryId already processed", "entryId");

        var merchant = await _store.GetMerchantAsync(request.Merchant!, cancellationToken);

        if (merchant?.IsDeleted != false)
            return Failures.Validation("Merchant not registered", "merchant");

        var operations = await _store.GetOperationsAsync("contract", merchant.Cnpj, cancellationToken);
        var contracts = operations.Where(operation => operation.Status == ScheduleQueryStatusType.PROCESSED
                && operation.SettlementBankAccountCode == request.BankAccount
                && operation.ResultJson.FromJson<ContractByExternalReference>()!.Status == ContractStatusType.Active)
            .ToArray();

        if (contracts.Length == 0)
            return Failures.Validation("bankAccount must be associated with an active API contract", "bankAccount");

        var allocated = await _allocations.AllocateAsync(merchant.Cnpj, request.Acquirer!, request.Value!.Value, contracts, cancellationToken);

        _store.AddEntry(new ReconciliationEntity
        {
            EntryId = id,
            MerchantCnpj = merchant.Cnpj,
            Payload = request.ToJson(),
            AllocationsJson = allocated.ToJson(),
            UnallocatedAmount = request.Value.Value - allocated.Sum(unit => unit.Amount),
        });

        await _store.SaveAsync(cancellationToken);

        return true;
    }
}
