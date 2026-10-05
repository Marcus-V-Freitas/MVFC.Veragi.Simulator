using System.Text.Json.Serialization;

namespace MVFC.Veragi.Simulator.Shareable.Serialization;

public sealed class TextEnumConverter<T>() : JsonStringEnumConverter<T>(allowIntegerValues: false)
    where T : struct, Enum;