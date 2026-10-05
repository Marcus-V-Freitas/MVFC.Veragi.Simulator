using MVFC.Veragi.Simulator.Domain.Entities;
using MVFC.Veragi.Simulator.Shareable.Configuration;
using MVFC.Veragi.Simulator.Domain.Services.Contracts;
using MVFC.Veragi.Simulator.Shareable.Enums;
using MVFC.Veragi.Simulator.Shareable.Extensions;
using MVFC.Veragi.Simulator.Shareable.Requests.Contracts;
using MVFC.Veragi.Simulator.Shareable.Requests.Schedules;
using MVFC.Veragi.Simulator.Shareable.Responses.Schedules;

namespace MVFC.Veragi.Simulator.Domain.Mappings;

public static class ScheduledOperationMappings
{
    public static ScheduledOperationEntity ToOperation(
        this ScheduleQueryRequest request,
        string merchantCnpj,
        string idempotencyKey,
        DateTime now,
        SimulatorOptions options
    )
    {
        var operation = Create("schedule", merchantCnpj, idempotencyKey, request.ToJson(), request.Fingerprint(), now, options);
        operation.ResultJson = new ScheduleQuery(Status: ScheduleQueryStatusType.PROCESSING, Detail: "Schedule query is processing", ScheduleQueryData: new Schedule(RequestId: operation.Id.ToString(), OriginType: ScheduleQueryOriginType.ONLINE, UpdatedAt: now.ToString("O"), MerchantCnpj: merchantCnpj)).ToJson();

        return operation;
    }

    public static ScheduledOperationEntity ToOperation(
        this ContractAnticipationCreateRequest request,
        string merchantCnpj,
        string idempotencyKey,
        DateTime now,
        SimulatorOptions options
    )
    {
        var operation = Create("contract", merchantCnpj, idempotencyKey, request.ToJson(), request.Fingerprint(), now, options);
        operation.ExternalReference = ContractIdentifierGenerator.Create(now);
        operation.ResultJson = request.ToDetails(operation.ExternalReference, ContractStatusType.PendingRegistration).ToJson();

        return operation;
    }

    private static ScheduledOperationEntity Create(string kind, string merchantCnpj, string idempotencyKey, string requestJson, string requestHash, DateTime now, SimulatorOptions options) => new()
    {
        Id = Guid.CreateVersion7(DateTimeOffset.UtcNow),
        Kind = kind,
        MerchantCnpj = merchantCnpj,
        IdempotencyKey = idempotencyKey,
        RequestJson = requestJson,
        RequestHash = requestHash,
        CreatedAt = now,
        DueAt = now.AddSeconds(options.ProcessingDelaySeconds),
        Outcome = ScheduleQueryStatusType.PROCESSED,
    };
}
