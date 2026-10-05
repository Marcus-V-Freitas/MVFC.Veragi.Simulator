using FluentAssertions;
using MVFC.Veragi.Simulator.Shareable.Results;
using Xunit;

namespace MVFC.Veragi.Simulator.Shareable.Tests.Results;

public sealed class FailuresTests
{
    [Fact]
    public void ValidationWithDefaultFieldCreatesProperException()
    {
        // Act
        var exception = Failures.Validation("Validation failed");

        // Assert
        exception.StatusCode.Should().Be(400);
        exception.Code.Should().Be("VALIDATION_FAILED");
        exception.Message.Should().Be("Validation failed");
        exception.Violations.Should().NotBeNull();
        exception.Violations.Should().ContainSingle();
        exception.Violations[0].Name.Should().Be("body");
        exception.Violations[0].Reason.Should().Be("Validation failed");
        exception.Violations[0].Location.Should().Be("body");
        exception.Violations[0].Path.Should().Be("$.body");
    }

    [Fact]
    public void ValidationWithCustomFieldSetsFieldInViolation()
    {
        // Act
        var exception = Failures.Validation("Required field", "cnpj");

        // Assert
        exception.StatusCode.Should().Be(400);
        exception.Violations[0].Name.Should().Be("cnpj");
        exception.Violations[0].Path.Should().Be("$.cnpj");
    }

    [Fact]
    public void MissingCreatesNotFoundException()
    {
        // Act
        var exception = Failures.Missing("Entity not found");

        // Assert
        exception.StatusCode.Should().Be(404);
        exception.Code.Should().Be("NOT_FOUND");
        exception.Message.Should().Be("Entity not found");
        exception.Violations.Should().BeEmpty();
    }

    [Fact]
    public void ConflictCreatesConflictException()
    {
        // Act
        var exception = Failures.Conflict("Entity already exists");

        // Assert
        exception.StatusCode.Should().Be(409);
        exception.Code.Should().Be("CONFLICT");
        exception.Message.Should().Be("Entity already exists");
        exception.Violations.Should().BeEmpty();
    }

    [Fact]
    public void UnprocessableCreatesUnprocessableEntityException()
    {
        // Act
        var exception = Failures.Unprocessable("Entity in invalid state");

        // Assert
        exception.StatusCode.Should().Be(422);
        exception.Code.Should().Be("UNPROCESSABLE_ENTITY");
        exception.Message.Should().Be("Entity in invalid state");
        exception.Violations.Should().BeEmpty();
    }
}
