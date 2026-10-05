using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using NSubstitute;
using MVFC.Veragi.Simulator.Domain.Validation;
using MVFC.Veragi.Simulator.Shareable.Requests.Schedules;
using MVFC.Veragi.Simulator.Shareable.Enums;
using Xunit;

namespace MVFC.Veragi.Simulator.Domain.Tests.Validation;

public sealed class RequestValidatorResolutionTests
{
    [Fact]
    public void ProviderRegisteredValidatorIsUsed()
    {
        // Arrange
        var provider = Substitute.For<IServiceProvider>();
        var validator = Substitute.For<IValidator<ScheduleQueryRequest>>();
        var request = new ScheduleQueryRequest(QueryType: ScheduleQueryType.STANDARD);
        validator.Validate(request).Returns(new ValidationResult());
        provider.GetService(typeof(IValidator<ScheduleQueryRequest>)).Returns(validator);
        var sut = new RequestValidator(provider);

        // Act
        var result = sut.Validate(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void ProviderWithoutRegisteredValidatorFallsBackToDefaultValidator()
    {
        // Arrange
        var provider = Substitute.For<IServiceProvider>();
        provider.GetService(typeof(IValidator<ScheduleQueryRequest>)).Returns((object?)null);
        var sut = new RequestValidator(provider);

        // Act
        var result = sut.Validate(new ScheduleQueryRequest(QueryType: ScheduleQueryType.STANDARD));

        // Assert
        result.IsSuccess.Should().BeFalse();
    }
}
