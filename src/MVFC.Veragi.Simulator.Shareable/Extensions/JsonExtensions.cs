using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace MVFC.Veragi.Simulator.Shareable.Extensions;

public static class JsonExtensions
{
    public static JsonSerializerOptions Options { get; } = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static string ToJson<T>(this T value) => JsonSerializer.Serialize(value, Options);

    public static T? FromJson<T>(this string value) => JsonSerializer.Deserialize<T>(value, Options);

    public static string Fingerprint<T>(this T value)
    {
        var node = JsonNode.Parse(value.ToJson());

        return Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(Canonical(node))));
    }

    private static string Canonical(JsonNode? node) => node switch
    {
        JsonObject obj => "{" + string.Join(',', obj.OrderBy(x => x.Key, StringComparer.Ordinal).Select(x => JsonSerializer.Serialize(x.Key) + ":" + Canonical(x.Value))) + "}",
        JsonArray array => "[" + string.Join(',', array.Select(Canonical)) + "]",
        _ => node?.ToJsonString() ?? "null",
    };
}
