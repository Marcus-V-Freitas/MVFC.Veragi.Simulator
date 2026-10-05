using FluentAssertions;
using Xunit;
using MVFC.Veragi.Simulator.Domain.Validation;
using MVFC.Veragi.Simulator.Shareable.Enums;
using MVFC.Veragi.Simulator.TestHelpers;
using MVFC.Veragi.Simulator.Shareable.Responses.Merchants;

namespace MVFC.Veragi.Simulator.Domain.Tests.Validation;

public sealed class ReleaseAccountValidationTests
{
    public static TheoryData<ReleaseAccount> InvalidRequests => new()
    {
        MockEntities.FullReleaseAccount()with
        {
            Id = null
        },
        MockEntities.FullReleaseAccount()with
        {
            Id = "invalid"
        },
        MockEntities.FullReleaseAccount()with
        {
            AccountType = null
        },
        MockEntities.FullReleaseAccount()with
        {
            AccountType = (ReleaseAccountType)999
        },
        MockEntities.FullReleaseAccount()with
        {
            BankCode = null
        },
        MockEntities.FullReleaseAccount()with
        {
            BankCode = "!invalid!"
        },
        MockEntities.FullReleaseAccount()with
        {
            Ispb = null
        },
        MockEntities.FullReleaseAccount()with
        {
            Ispb = "!invalid!"
        },
        MockEntities.FullReleaseAccount()with
        {
            Branch = null
        },
        MockEntities.FullReleaseAccount()with
        {
            Branch = "!invalid!"
        },
        MockEntities.FullReleaseAccount()with
        {
            Account = null
        },
        MockEntities.FullReleaseAccount()with
        {
            Account = "!invalid!"
        },
        MockEntities.FullReleaseAccount()with
        {
            ExternalId = new string ('a', 41)
        },
    };

    public static TheoryData<ReleaseAccount> ValidRequests => new()
    {
        MockEntities.FullReleaseAccount(),
        MockEntities.FullReleaseAccount()with
        {
            ExternalId = null
        },
    };

    [Theory]
    [MemberData(nameof(InvalidRequests))]
    public void InvalidFieldProducesValidationFailure(ReleaseAccount request)
    {
        // Arrange
        var validator = new ReleaseAccountValidator();

        // Act
        var result = validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
    }

    [Theory]
    [MemberData(nameof(ValidRequests))]
    public void ValidAndOmittedOptionalFieldsAreAccepted(ReleaseAccount request)
    {
        // Arrange
        var validator = new ReleaseAccountValidator();

        // Act
        var result = validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }
}
