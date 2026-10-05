using System.Text.Json.Serialization;
using MVFC.Veragi.Simulator.Shareable.Enums;

namespace MVFC.Veragi.Simulator.Shareable.Responses.Contracts;

public sealed record SettlementAccountContract(
    [property: JsonPropertyName("document")] string? Document = null,
    [property: JsonPropertyName("accountType")] AccountType? AccountType = null,
    [property: JsonPropertyName("compe")] string? Compe = null,
    [property: JsonPropertyName("ispb")] string? Ispb = null,
    [property: JsonPropertyName("branch")] string? Branch = null,
    [property: JsonPropertyName("account")] string? Account = null
);
