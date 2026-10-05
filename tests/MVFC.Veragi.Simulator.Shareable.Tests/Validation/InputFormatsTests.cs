using FluentAssertions;
using MVFC.Veragi.Simulator.Shareable.Extensions;
using Xunit;

namespace MVFC.Veragi.Simulator.Shareable.Tests.Validation;

public sealed class InputFormatsTests
{
    [Theory]
    [InlineData(null, false, false)]
    [InlineData(null, true, false)]
    [InlineData("999", false, false)]
    [InlineData("999", true, true)]
    [InlineData("VCC", false, true)]
    [InlineData("VCC", true, true)]
    [InlineData("VC", true, false)]
    [InlineData("VCCC", false, false)]
    [InlineData("@CC", false, false)]
    [InlineData("[CC", false, false)]
    [InlineData("vCC", false, false)]
    public void ArrangementRequiresThreeUppercaseLettersOrEnabledWildcard(string? value, bool allowWildcard, bool expected)
    {
        // Arrange
        var arrangement = value;

        // Act
        var accepted = InputFormats.IsArrangement(arrangement, allowWildcard);

        // Assert
        accepted.Should().Be(expected);
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("1", false)]
    [InlineData("123456", false)]
    [InlineData("123", true)]
    [InlineData("12345", true)]
    [InlineData("12A", false)]
    public void NumericFieldEnforcesBothLengthBoundariesAndDigits(string? value, bool expected)
    {
        // Arrange
        const int minimum = 3;
        const int maximum = 5;

        // Act
        var accepted = InputFormats.IsDigits(value, minimum, maximum);

        // Assert
        accepted.Should().Be(expected);
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("A", false)]
    [InlineData("ABC123", false)]
    [InlineData("ABC", true)]
    [InlineData("a1234", true)]
    [InlineData("AB-", false)]
    [InlineData("ABé", false)]
    public void AlphaNumericFieldEnforcesLengthAndAsciiCharacters(string? value, bool expected)
    {
        // Arrange
        const int minimum = 3;
        const int maximum = 5;

        // Act
        var accepted = InputFormats.IsAlphaNumeric(value, minimum, maximum);

        // Assert
        accepted.Should().Be(expected);
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("123", false)]
    [InlineData("123456789012345", false)]
    [InlineData("12345678000195", true)]
    [InlineData("ABCD1234EF0195", true)]
    [InlineData("1234567800019A", false)]
    [InlineData("12345678000!95", false)]
    public void CnpjFieldEnforcesTwelveAlphanumericsAndTwoDigits(string? value, bool expected)
    {
        // Act
        var accepted = InputFormats.IsCnpj(value);

        // Assert
        accepted.Should().Be(expected);
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("invalid-date", false)]
    [InlineData("04/10/2026", false)]
    [InlineData("2026-10-04", true)]
    public void DateFieldEnforcesIsoDateFormat(string? value, bool expected)
    {
        // Act
        var accepted = InputFormats.IsDate(value);

        // Assert
        accepted.Should().Be(expected);
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("invalid-timestamp", false)]
    [InlineData("2026-10-04T19:55:00Z", true)]
    [InlineData("2026-10-04T19:55:00-03:00", true)]
    public void TimestampFieldEnforcesParsableIsoTimestamp(string? value, bool expected)
    {
        // Act
        var accepted = InputFormats.IsTimestamp(value);

        // Assert
        accepted.Should().Be(expected);
    }
}

