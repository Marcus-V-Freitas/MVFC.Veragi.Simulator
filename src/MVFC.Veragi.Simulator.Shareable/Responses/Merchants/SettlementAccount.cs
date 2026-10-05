using System.Text.Json.Serialization;
using MVFC.Veragi.Simulator.Shareable.Enums;

namespace MVFC.Veragi.Simulator.Shareable.Responses.Merchants;

public sealed record SettlementAccount(
    [property: JsonPropertyName("cnpjRecipient")] string? CnpjRecipient = null,
    [property: JsonPropertyName("corporateNameRecipient")] string? CorporateNameRecipient = null,
    [property: JsonPropertyName("accountType")] AccountType? AccountType = null,
    [property: JsonPropertyName("bankCode")] string? BankCode = null,
    [property: JsonPropertyName("ispb")] string? Ispb = null,
    [property: JsonPropertyName("branch")] string? Branch = null,
    [property: JsonPropertyName("account")] string? Account = null
);
