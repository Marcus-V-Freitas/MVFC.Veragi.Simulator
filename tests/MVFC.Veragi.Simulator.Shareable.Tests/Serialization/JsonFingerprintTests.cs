using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using FluentAssertions;
using MVFC.Veragi.Simulator.Shareable.Extensions;
using Xunit;

namespace MVFC.Veragi.Simulator.Shareable.Tests.Serialization;

public sealed class JsonFingerprintTests
{
    [Theory]
    [InlineData("null")]
    [InlineData("42")]
    [InlineData("\"value\"")]
    [InlineData("true")]
    [InlineData("[null,1]")]
    public void FingerprintUsesCanonicalJsonForNullAndScalarOrArrayPayloads(string payload)
    {
        // Arrange
        var value = JsonNode.Parse(payload);
        var expected = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload)));

        // Act
        var fingerprint = value.Fingerprint();

        // Assert
        fingerprint.Should().Be(expected);
    }
}
