using MVFC.Veragi.Simulator.Domain.Services.Catalogs;
using MVFC.Veragi.Simulator.Domain.Services.Common;
using MVFC.Veragi.Simulator.Shareable.Requests.Merchants;
using MVFC.Veragi.Simulator.Shareable.Requests.Sales;
using MVFC.Veragi.Simulator.Shareable.Responses.Merchants;
using MVFC.Veragi.Simulator.Shareable.Responses.Sales;
using OperationResult;
using System.Globalization;
using MVFC.Veragi.Simulator.Domain.Entities;
using MVFC.Veragi.Simulator.Domain.Mappings;
using MVFC.Veragi.Simulator.Domain.Ports;
using MVFC.Veragi.Simulator.Shareable.Configuration;
using MVFC.Veragi.Simulator.Shareable.Extensions;
using MVFC.Veragi.Simulator.Shareable.Results;

namespace MVFC.Veragi.Simulator.Domain.Services.Sales;

public sealed class SalesService(
    ISimulatorStore store,
    SimulationGate gate,
    CatalogService catalog,
    BusinessCalendar calendar,
    SimulatorOptions options,
    TimeProvider clock
)
{
    private readonly ISimulatorStore _store = store;
    private readonly SimulationGate _gate = gate;
    private readonly CatalogService _catalog = catalog;
    private readonly BusinessCalendar _calendar = calendar;
    private readonly SimulatorOptions _options = options;
    private readonly TimeProvider _clock = clock;

    public async Task<Result<GenerateSalesResponse>> GenerateAsync(
        string cnpj,
        GenerateSalesRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken
    )
    {
        using var lease = await _gate.EnterAsync(cancellationToken);

        if (!CatalogService.IsCnpj(cnpj))
            return Failures.Validation("Invalid merchant CNPJ", "cnpj");

        if (!Guid.TryParse(idempotencyKey, out var key))
            return Failures.Validation("Idempotency-Key must be a UUID", "Idempotency-Key");

        var merchantState = await _store.GetMerchantAsync(cnpj, cancellationToken);

        if (merchantState?.IsDeleted != false)
            return Failures.Missing("Merchant not found");

        var batchKey = cnpj + ":" + key;
        var existing = await _store.GetSalesBatchAsync(batchKey, cancellationToken);

        if (existing is not null)
            return existing.RequestHash == request.Fingerprint() ? Result.Success(existing.ResponseJson.FromJson<GenerateSalesResponse>()!) : Failures.Conflict("Sales key reused with different payload");

        var config = merchantState.Payload.FromJson<Merchant>()!.ReceivablesScheduleConfig!;
        var configuredAcquirers = config.AcquirerCnpjs!.Contains("99999999000199") ? [.. _catalog.Acquirers.Select(x => x.Cnpj!)] : config.AcquirerCnpjs;
        var configuredArrangements = config.ArrangementCodes!.Contains("999") ? [.. _catalog.Arrangements.Select(x => x.Code!)] : config.ArrangementCodes;
        var batchId = Guid.CreateVersion7(DateTimeOffset.UtcNow);
        var now = _clock.GetUtcNow().UtcDateTime;
        var built = request.Sales is null ? BuildCompact(request, configuredAcquirers, configuredArrangements) : BuildExplicit(request.Sales, configuredAcquirers, configuredArrangements);

        if (!built.IsSuccess)
            return built.Exception!;

        var installments = built.Value;

        foreach (var installment in installments)
        {
            installment.BatchId = batchId;
            installment.MerchantCnpj = cnpj;
            installment.CreatedAt = now;
            _store.AddSale(installment);
        }

        var response = new GenerateSalesResponse(batchId, cnpj, installments.Select(x => x.SaleId).Distinct().Count(), installments.Sum(x => x.Amount), installments.Min(x => x.SettlementDate)!, installments.Max(x => x.SettlementDate)!, installments.Count);
        _store.AddSalesBatch(new SalesBatchEntity { BatchKey = batchKey, MerchantCnpj = cnpj, RequestHash = request.Fingerprint(), ResponseJson = response.ToJson() });
        await _store.SaveAsync(cancellationToken);

        return response;
    }

    private Result<List<SimulatedSaleEntity>> BuildExplicit(List<SaleCreateRequest> sales, List<string> acquirers, List<string> arrangements)
    {
        if (sales.Count is < 1 or > 5000 || sales.Exists(x => x is null || x.Installments is null) || sales.Sum(x => (long)x.Installments!.Count) is < 1 or > 5000)
            return Failures.Validation("A batch must contain sales and at most 5000 installments");

        var externalIds = new HashSet<string>(StringComparer.Ordinal);
        var result = new List<SimulatedSaleEntity>();

        foreach (var sale in sales)
        {
            if (!acquirers.Contains(sale.AcquirerCnpj) || !arrangements.Contains(sale.PaymentArrangementCode))
                return Failures.Validation("Sales must use the merchant registered acquirers and arrangements");

            if (sale.ExternalId is not null && (string.IsNullOrWhiteSpace(sale.ExternalId) || sale.ExternalId.Length > 64 || !externalIds.Add(sale.ExternalId)))
                return Failures.Validation("externalId must be unique within the batch and contain 1..64 characters");

            if (sale.Installments!.Count == 0)
                return Failures.Validation("Each sale must contain at least one installment");

            var saleId = Guid.CreateVersion7(DateTimeOffset.UtcNow);
            for (var index = 0; index < sale.Installments!.Count; index++)
            {
                var installment = sale.Installments[index];

                if (installment is null || !ValidAmount(installment.Amount))
                    return Failures.Validation("Each installment amount must be positive, at most 10000000 and have at most two decimal places");

                if ((installment.SettlementDate is null) == (installment.SettlementBusinessDays is null))
                    return Failures.Validation("Choose exactly one of settlementDate or settlementBusinessDays per installment");

                DateOnly date;

                if (installment.SettlementBusinessDays is { } days)
                {
                    if (days is < 0 or > 730)
                        return Failures.Validation("settlementBusinessDays must be 0..730");

                    date = _calendar.AddBusinessDays(_calendar.Today, days);
                }
                else if (!DateOnly.TryParseExact(installment.SettlementDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
                {
                    return Failures.Validation("Invalid installment settlementDate");
                }

                if (date < _calendar.Today || date > _calendar.Today.AddYears(5))
                    return Failures.Validation("Settlement date must be between today and five years from today");

                result.Add(new SimulatedSaleEntity { Id = Guid.CreateVersion7(DateTimeOffset.UtcNow), SaleId = saleId, InstallmentNumber = index + 1, ExternalId = sale.ExternalId, AcquirerCnpj = sale.AcquirerCnpj, PaymentArrangementCode = sale.PaymentArrangementCode, SettlementDate = date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), Amount = installment.Amount });
            }
        }

        return result;
    }

    private Result<List<SimulatedSaleEntity>> BuildCompact(GenerateSalesRequest request, List<string> configuredAcquirers, List<string> configuredArrangements)
    {
        if (request.Days is < 1 or > 365 || request.SalesPerDay is < 1 or > 1000)
            return Failures.Validation("days must be 1..365 and salesPerDay 1..1000");

        if (!ValidAmount(request.AmountPerSale))
            return Failures.Validation("amountPerSale must be positive, at most 10000000 and have at most two decimal places");

        var start = _calendar.AddBusinessDays(_calendar.Today, _options.MinimumBusinessDays);

        if (request.StartSettlementDate is not null && !DateOnly.TryParseExact(request.StartSettlementDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out start))
            return Failures.Validation("Invalid startSettlementDate");

        if (start.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday || _options.Holidays.Contains(start.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)))
            return Failures.Validation("startSettlementDate must be a business day");

        if (start < _calendar.Today)
            return Failures.Validation("startSettlementDate must not be in the past");

        var acquirers = request.AcquirerCnpjs ?? configuredAcquirers;
        var arrangements = request.ArrangementCodes ?? configuredArrangements;

        if (acquirers.Count == 0 || arrangements.Count == 0 || acquirers.Distinct().Count() != acquirers.Count || arrangements.Distinct().Count() != arrangements.Count)
            return Failures.Validation("Acquirers and arrangements must be nonempty and unique");

        if (acquirers.Exists(x => !configuredAcquirers.Contains(x)) || arrangements.Exists(x => !configuredArrangements.Contains(x)))
            return Failures.Validation("Sales must use the merchant registered acquirers and arrangements");

        var count = (long)request.Days * request.SalesPerDay * acquirers.Count * arrangements.Count;

        if (count > 5000)
            return Failures.Validation("A batch cannot exceed 5000 installments");

        var result = new List<SimulatedSaleEntity>();
        for (var day = 0; day < request.Days; day++)
        {
            foreach (var acquirer in acquirers)
            {
                foreach (var arrangement in arrangements)
                {
                    for (var sale = 0; sale < request.SalesPerDay; sale++)
                    {
                        result.Add(new SimulatedSaleEntity { Id = Guid.CreateVersion7(DateTimeOffset.UtcNow), SaleId = Guid.CreateVersion7(DateTimeOffset.UtcNow), InstallmentNumber = 1, AcquirerCnpj = acquirer, PaymentArrangementCode = arrangement, SettlementDate = _calendar.AddBusinessDays(start, day).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), Amount = request.AmountPerSale });
                    }
                }
            }
        }

        return result;
    }

    private static bool ValidAmount(decimal amount) =>
        amount is >= 0.01m and <= 10000000m && amount == decimal.Round(amount, 2, MidpointRounding.ToEven);

    public async Task<Result<IReadOnlyList<SimulatedSaleResponse>>> ListAsync(
        string cnpj,
        CancellationToken cancellationToken
    )
    {
        if (!CatalogService.IsCnpj(cnpj))
            return Failures.Validation("Invalid merchant CNPJ", "cnpj");

        var merchant = await _store.GetMerchantAsync(cnpj, cancellationToken);

        if (merchant?.IsDeleted != false)
            return Failures.Missing("Merchant not found");

        var sales = await _store.GetSalesAsync(cnpj, cancellationToken);

        return Result.Success<IReadOnlyList<SimulatedSaleResponse>>([.. sales.OrderBy(x => x.SettlementDate).Select(x => x.ToResponse())]);
    }
}
