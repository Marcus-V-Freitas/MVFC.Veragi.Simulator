using OperationResult;
using System.Globalization;
using MVFC.Veragi.Simulator.Domain.Entities;
using MVFC.Veragi.Simulator.Domain.Ports;
using MVFC.Veragi.Simulator.Domain.Services.Catalogs;
using MVFC.Veragi.Simulator.Domain.Services.Common;
using MVFC.Veragi.Simulator.Domain.Services.Receivables;
using MVFC.Veragi.Simulator.Shareable.Extensions;
using MVFC.Veragi.Simulator.Shareable.Requests.Reconciliation;
using MVFC.Veragi.Simulator.Shareable.Responses.Merchants;
using MVFC.Veragi.Simulator.Shareable.Responses.Reconciliation;
using MVFC.Veragi.Simulator.Shareable.Results;

namespace MVFC.Veragi.Simulator.Domain.Services.Reconciliation;

public sealed class ExternalAnticipationService(ISimulatorStore store, SimulationGate gate)
{
    private readonly ISimulatorStore _store = store;
    private readonly SimulationGate _gate = gate;

    public async Task<Result<ExternalAnticipationResponse>> CreateAsync(
        string cnpj,
        ExternalAnticipationRequest request,
        string idempotencyKey,
        CancellationToken ct
    )
    {
        using var lease = await _gate.EnterAsync(ct);

        if (!CatalogService.IsCnpj(cnpj) || !Guid.TryParse(idempotencyKey, out var key))
            return Failures.Validation("Invalid merchant CNPJ or Idempotency-Key");

        if (!CatalogService.IsCnpj(request.AcquirerCnpj ?? "") || string.IsNullOrEmpty(request.PaymentArrangementCode) ||
            !DateOnly.TryParseExact(request.SettlementDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _) ||
            request.Amount <= 0 || request.Amount != decimal.Round(request.Amount, 2, MidpointRounding.ToEven))
        {
            return Failures.Validation("Invalid receivable or amount");
        }

        var merchant = await _store.GetMerchantAsync(cnpj, ct);

        if (merchant?.IsDeleted != false)
            return Failures.Missing("Merchant not found");

        var items = await _store.GetExternalAnticipationsAsync(cnpj, ct);
        var existing = items.FirstOrDefault(x => x.IdempotencyKey == key.ToString());

        if (existing is not null)
            return existing.RequestHash == request.Fingerprint() ? Result.Success(ToResponse(existing)) : Failures.Conflict("Idempotency-Key reused with another anticipation");

        var unitKey = ReceivableBalances.Key(cnpj, request.AcquirerCnpj!, request.PaymentArrangementCode!, request.SettlementDate!);
        var sales = await _store.GetSalesAsync(cnpj, ct);
        var total = sales.Where(x => ReceivableBalances.Key(x.MerchantCnpj, x.AcquirerCnpj, x.PaymentArrangementCode, x.SettlementDate) == unitKey).Sum(x => x.Amount);
        var committed = ReceivableBalances.Commitments(await _store.GetOperationsAsync("contract", cnpj, ct), includePending: false).GetValueOrDefault(unitKey) + items.Where(x => ReceivableBalances.Key(cnpj, x.AcquirerCnpj, x.PaymentArrangementCode, x.SettlementDate) == unitKey).Sum(x => x.Amount);

        if (request.Amount > Math.Max(0, total - committed))
            return Failures.Unprocessable("External anticipation exceeds available receivables");

        var item = new ExternalAnticipationEntity
        {
            Id = Guid.CreateVersion7(DateTimeOffset.UtcNow),
            MerchantCnpj = cnpj,
            AcquirerCnpj = request.AcquirerCnpj!,
            PaymentArrangementCode = request.PaymentArrangementCode!,
            SettlementDate = request.SettlementDate!,
            Amount = request.Amount,
            IdempotencyKey = key.ToString(),
            RequestHash = request.Fingerprint(),
        };
        _store.AddExternalAnticipation(item);
        await _store.SaveAsync(ct);

        return ToResponse(item);
    }

    private static ExternalAnticipationResponse ToResponse(ExternalAnticipationEntity item) => new(item.Id, item.MerchantCnpj, item.AcquirerCnpj, item.PaymentArrangementCode, item.SettlementDate, item.Amount);
}
