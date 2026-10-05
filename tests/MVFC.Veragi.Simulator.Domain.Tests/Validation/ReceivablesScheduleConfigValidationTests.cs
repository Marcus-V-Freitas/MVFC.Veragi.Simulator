using FluentAssertions;
using Xunit;
using MVFC.Veragi.Simulator.Domain.Validation;
using MVFC.Veragi.Simulator.Shareable.Enums;
using MVFC.Veragi.Simulator.TestHelpers;
using MVFC.Veragi.Simulator.Shareable.Requests.Merchants;

namespace MVFC.Veragi.Simulator.Domain.Tests.Validation;

public sealed class ReceivablesScheduleConfigValidationTests
{
    public static TheoryData<ReceivablesScheduleConfig> InvalidRequests =>
    [
        MockEntities.FullReceivablesScheduleConfig()with
        {
            QueryWindow = null
        },
        MockEntities.FullReceivablesScheduleConfig()with
        {
            QueryWindow = (QueryWindowType)999
        },
        MockEntities.FullReceivablesScheduleConfig()with
        {
            AcquirerCnpjs = null
        },
        MockEntities.FullReceivablesScheduleConfig()with
        {
            AcquirerCnpjs = []
        },
        MockEntities.FullReceivablesScheduleConfig()with
        {
            AcquirerCnpjs = ["invalid"]
        },
        MockEntities.FullReceivablesScheduleConfig()with
        {
            AcquirerCnpjs = [null !]
        },
        MockEntities.FullReceivablesScheduleConfig()with
        {
            ArrangementCodes = null
        },
        MockEntities.FullReceivablesScheduleConfig()with
        {
            ArrangementCodes = []
        },
        MockEntities.FullReceivablesScheduleConfig()with
        {
            ArrangementCodes = ["invalid"]
        },
        MockEntities.FullReceivablesScheduleConfig()with
        {
            ArrangementCodes = [null !]
        },
    ];

    public static TheoryData<ReceivablesScheduleConfig> ValidRequests =>
    [
        MockEntities.FullReceivablesScheduleConfig(),
    ];

    [Theory]
    [MemberData(nameof(InvalidRequests))]
    public void InvalidFieldProducesValidationFailure(ReceivablesScheduleConfig request)
    {
        // Arrange
        var validator = new ReceivablesScheduleConfigValidator();

        // Act
        var result = validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
    }

    [Theory]
    [MemberData(nameof(ValidRequests))]
    public void ValidAndOmittedOptionalFieldsAreAccepted(ReceivablesScheduleConfig request)
    {
        // Arrange
        var validator = new ReceivablesScheduleConfigValidator();

        // Act
        var result = validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }
}
