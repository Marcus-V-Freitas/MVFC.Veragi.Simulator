using System.Globalization;

namespace MVFC.Veragi.Simulator.Shareable.Extensions;

public static class InputFormats
{
    public static bool IsCnpj(string? value) => value is { Length: 14 } && value.Take(12).All(char.IsAsciiLetterOrDigit) && value.Skip(12).All(char.IsDigit);

    public static bool IsArrangement(
        string? value,
        bool allowWildcard = false
    ) => (allowWildcard && value == "999") || (value is { Length: 3 } && value.All(character => character is >= 'A' and <= 'Z'));

    public static bool IsDigits(
        string? value,
        int minimum,
        int maximum
    ) => value is not null && value.Length >= minimum && value.Length <= maximum && value.All(char.IsDigit);

    public static bool IsAlphaNumeric(
        string? value,
        int minimum,
        int maximum
    ) => value is not null && value.Length >= minimum && value.Length <= maximum && value.All(char.IsAsciiLetterOrDigit);

    public static bool IsDate(string? value) => DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _);

    public static bool IsTimestamp(string? value) => DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out _);
}
