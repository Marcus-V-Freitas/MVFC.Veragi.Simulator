using MVFC.Veragi.Simulator.Shareable.Requests.Merchants;
using System.Text.Json.Serialization;
using MVFC.Veragi.Simulator.Shareable.Enums;

namespace MVFC.Veragi.Simulator.Shareable.Responses.Merchants;

public sealed record Merchant(
    [property: JsonPropertyName("corporateName")] string? CorporateName = null,
    [property: JsonPropertyName("cnpj")] string? Cnpj = null,
    [property: JsonPropertyName("email")] string? Email = null,
    [property: JsonPropertyName("mobilePhone")] string? MobilePhone = null,
    [property: JsonPropertyName("receivablesScheduleConfig")] ReceivablesScheduleConfig? ReceivablesScheduleConfig = null,
    [property: JsonPropertyName("creditConfigurations")] List<CreditConfigurationType>? CreditConfigurations = null,
    [property: JsonPropertyName("limitType")] LimitType? LimitType = null,
    [property: JsonPropertyName("limitAmount")] decimal? LimitAmount = null,
    [property: JsonPropertyName("releaseAccounts")] List<ReleaseAccount>? ReleaseAccounts = null,
    [property: JsonPropertyName("creditSettlementAccount")] SettlementAccount? CreditSettlementAccount = null,
    [property: JsonPropertyName("anticipationSettlementAccount")] SettlementAccount? AnticipationSettlementAccount = null
);
