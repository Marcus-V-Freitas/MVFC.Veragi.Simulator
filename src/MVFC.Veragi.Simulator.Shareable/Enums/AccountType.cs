using System.Text.Json.Serialization;
using MVFC.Veragi.Simulator.Shareable.Serialization;

namespace MVFC.Veragi.Simulator.Shareable.Enums;

[JsonConverter(typeof(TextEnumConverter<AccountType>))]
public enum AccountType
{
    CONTA_DEPOSITO_A_VISTA,
    CONTA_PAGAMENTO_PRE_PAGA,
    CONTA_POUPANCA,
    CONTA_DEPOSITO,
}
