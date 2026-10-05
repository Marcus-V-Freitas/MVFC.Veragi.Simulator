using System.Text.Json.Serialization;
using MVFC.Veragi.Simulator.Shareable.Enums;

namespace MVFC.Veragi.Simulator.Shareable.Requests.Merchants;

public sealed record ReceivablesScheduleConfig(
    [property: JsonPropertyName("queryWindow")] QueryWindowType? QueryWindow = null,
    [property: JsonPropertyName("acquirerCnpjs")] List<string>? AcquirerCnpjs = null,
    [property: JsonPropertyName("arrangementCodes")] List<string>? ArrangementCodes = null
);
