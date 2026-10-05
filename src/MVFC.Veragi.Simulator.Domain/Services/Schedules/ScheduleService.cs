using MVFC.Veragi.Simulator.Domain.Services.Common;
using MVFC.Veragi.Simulator.Domain.Mappings;
using MVFC.Veragi.Simulator.Shareable.Requests.Schedules;
using MVFC.Veragi.Simulator.Shareable.Responses.Schedules;
using MVFC.Veragi.Simulator.Domain.Validation;
using OperationResult;
using MVFC.Veragi.Simulator.Domain.Ports;
using MVFC.Veragi.Simulator.Shareable.Configuration;
using MVFC.Veragi.Simulator.Shareable.Enums;
using MVFC.Veragi.Simulator.Shareable.Extensions;
using MVFC.Veragi.Simulator.Shareable.Results;

namespace MVFC.Veragi.Simulator.Domain.Services.Schedules;

public sealed class ScheduleService(
    ISimulatorStore store,
    RequestValidator validator,
    SimulationGate gate,
    SimulatorOptions options,
    TimeProvider clock
)
{
    private readonly ISimulatorStore _store = store;
    private readonly RequestValidator _validator = validator;
    private readonly SimulationGate _gate = gate;
    private readonly SimulatorOptions _options = options;
    private readonly TimeProvider _clock = clock;

    public async Task<Result<ScheduleQueryResponse>> SubmitAsync(
        ScheduleQueryRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken
    )
    {
        using var lease = await _gate.EnterAsync(cancellationToken);
        var valid = _validator.Validate(request);

        if (!valid.IsSuccess)
            return valid.Exception!;

        if (!Guid.TryParse(idempotencyKey, out _))
            return Failures.Validation("Idempotency-Key must be a UUID", "Idempotency-Key");

        idempotencyKey = Guid.Parse(idempotencyKey).ToString();

        if (request.QueryType != ScheduleQueryType.STANDARD)
            return Failures.Unprocessable("EXPLORATORY is not supported");

        var merchant = await _store.GetMerchantAsync(request.MerchantCnpj!, cancellationToken);

        if (merchant?.IsDeleted != false)
            return Failures.Missing("Merchant not found");

        var operations = await _store.GetOperationsAsync("schedule", merchant.Cnpj, cancellationToken);
        var existing = operations.FirstOrDefault(x => x.IdempotencyKey == idempotencyKey);

        if (existing is not null)
            return new ScheduleQueryResponse(RequestId: existing.Id.ToString());

        if (operations.Exists(x => x.Status == ScheduleQueryStatusType.PROCESSING))
            return Failures.Conflict("An active schedule query already exists");

        var now = _clock.GetUtcNow().UtcDateTime;
        var operation = request.ToOperation(merchant.Cnpj, idempotencyKey, now, _options);
        _store.AddOperation(operation);
        await _store.SaveAsync(cancellationToken);

        return new ScheduleQueryResponse(RequestId: operation.Id.ToString());
    }

    public async Task<Result<ScheduleQuery>> GetAsync(string uuid, CancellationToken cancellationToken)
    {
        if (!Guid.TryParseExact(uuid, "D", out var id) || id.Version is not 4 and not 7)
            return Failures.Validation("uuid must be a UUID v4 or v7", "uuid");

        var operation = await _store.GetOperationAsync(id, cancellationToken);

        return operation is null || operation.Kind != "schedule" ? Failures.Missing("Schedule not found") : Result.Success(operation.ResultJson.FromJson<ScheduleQuery>()!);
    }
}
