using FluentAssertions;
using Xunit;
using MVFC.Veragi.Simulator.Domain.Validation;
using MVFC.Veragi.Simulator.Shareable.Enums;
using MVFC.Veragi.Simulator.TestHelpers;
using MVFC.Veragi.Simulator.Shareable.Requests.Schedules;

namespace MVFC.Veragi.Simulator.Domain.Tests.Validation;

public sealed class ScheduleQueryRequestValidationTests
{
    public static TheoryData<ScheduleQueryRequest> InvalidRequests =>
    [
        MockEntities.FullScheduleQueryRequest()with
        {
            MerchantCnpj = null
        },
        MockEntities.FullScheduleQueryRequest()with
        {
            MerchantCnpj = "!invalid!"
        },
        MockEntities.FullScheduleQueryRequest()with
        {
            QueryType = null
        },
        MockEntities.FullScheduleQueryRequest()with
        {
            QueryType = (ScheduleQueryType)999
        },
    ];

    public static TheoryData<ScheduleQueryRequest> ValidRequests =>
    [
        MockEntities.FullScheduleQueryRequest(),
    ];

    [Theory]
    [MemberData(nameof(InvalidRequests))]
    public void InvalidFieldProducesValidationFailure(ScheduleQueryRequest request)
    {
        // Arrange
        var validator = new ScheduleQueryRequestValidator();

        // Act
        var result = validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
    }

    [Theory]
    [MemberData(nameof(ValidRequests))]
    public void ValidAndOmittedOptionalFieldsAreAccepted(ScheduleQueryRequest request)
    {
        // Arrange
        var validator = new ScheduleQueryRequestValidator();

        // Act
        var result = validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }
}
