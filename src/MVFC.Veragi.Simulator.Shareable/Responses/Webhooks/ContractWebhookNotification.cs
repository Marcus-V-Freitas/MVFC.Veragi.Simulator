using System.Text.Json.Serialization;
using MVFC.Veragi.Simulator.Shareable.Enums;

namespace MVFC.Veragi.Simulator.Shareable.Responses.Webhooks;

public sealed record ContractWebhookNotification(
    [property: JsonRequired] ScheduleQueryStatusType Status,
    string Detail,
    ContractWebhookData ScheduleQueryData
);
