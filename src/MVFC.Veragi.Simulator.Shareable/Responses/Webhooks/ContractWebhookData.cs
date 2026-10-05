using System.Text.Json.Serialization;
using MVFC.Veragi.Simulator.Shareable.Enums;

namespace MVFC.Veragi.Simulator.Shareable.Responses.Webhooks;

public sealed record ContractWebhookData(
    string RequestId,
    ScheduleQueryOriginType OriginType,
    string UpdatedAt,
    string MerchantCnpj,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.Never)] IReadOnlyList<ContractWebhookAcquirer>? Acquirers
);
