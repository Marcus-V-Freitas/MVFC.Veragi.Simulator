using System.Text.Json.Serialization;
using MVFC.Veragi.Simulator.Shareable.Serialization;

namespace MVFC.Veragi.Simulator.Shareable.Enums;

[JsonConverter(typeof(TextEnumConverter<ContractSituationType>))]
public enum ContractSituationType
{
    ACTIVE,
}
