using System.Text.Json.Serialization;
using MVFC.Veragi.Simulator.Shareable.Serialization;

namespace MVFC.Veragi.Simulator.Shareable.Enums;

[JsonConverter(typeof(TextEnumConverter<ContractAvailabilityMode>))]
public enum ContractAvailabilityMode
{
    IMMEDIATE = 1,
    DEFERRED = 2,
}
