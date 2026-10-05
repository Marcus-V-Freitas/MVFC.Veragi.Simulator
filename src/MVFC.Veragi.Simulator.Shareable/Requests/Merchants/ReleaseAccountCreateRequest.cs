using System.Text.Json.Serialization;
using MVFC.Veragi.Simulator.Shareable.Enums;

namespace MVFC.Veragi.Simulator.Shareable.Requests.Merchants;

public sealed record ReleaseAccountCreateRequest(
    [property: JsonPropertyName("accountType")] ReleaseAccountType? AccountType = null,
    [property: JsonPropertyName("bankCode")] string? BankCode = null,
    [property: JsonPropertyName("ispb")] string? Ispb = null,
    [property: JsonPropertyName("branch")] string? Branch = null,
    [property: JsonPropertyName("account")] string? Account = null,
    [property: JsonPropertyName("externalId")] string? ExternalId = null
);
