using FluentAssertions;
using Xunit;
using MVFC.Veragi.Simulator.Domain.Validation;
using MVFC.Veragi.Simulator.Shareable.Enums;
using MVFC.Veragi.Simulator.TestHelpers;
using MVFC.Veragi.Simulator.Shareable.Requests.Merchants;

namespace MVFC.Veragi.Simulator.Domain.Tests.Validation;

public sealed class ReleaseAccountCreateRequestValidationTests
{
    public static TheoryData<ReleaseAccountCreateRequest> InvalidRequests =>
    [
        MockEntities.FullReleaseAccountCreateRequest()with
        {
            AccountType = null
        },
        MockEntities.FullReleaseAccountCreateRequest()with
        {
            AccountType = (ReleaseAccountType)999
        },
        MockEntities.FullReleaseAccountCreateRequest()with
        {
            BankCode = null
        },
        MockEntities.FullReleaseAccountCreateRequest()with
        {
            BankCode = "!invalid!"
        },
        MockEntities.FullReleaseAccountCreateRequest()with
        {
            Ispb = null
        },
        MockEntities.FullReleaseAccountCreateRequest()with
        {
            Ispb = "!invalid!"
        },
        MockEntities.FullReleaseAccountCreateRequest()with
        {
            Branch = null
        },
        MockEntities.FullReleaseAccountCreateRequest()with
        {
            Branch = "!invalid!"
        },
        MockEntities.FullReleaseAccountCreateRequest()with
        {
            Account = null
        },
        MockEntities.FullReleaseAccountCreateRequest()with
        {
            Account = "!invalid!"
        },
        MockEntities.FullReleaseAccountCreateRequest()with
        {
            ExternalId = new string ('a', 41)
        },
    ];

    public static TheoryData<ReleaseAccountCreateRequest> ValidRequests => new()
    {
        MockEntities.FullReleaseAccountCreateRequest(),
        MockEntities.FullReleaseAccountCreateRequest()with
        {
            ExternalId = null
        },
    };

    [Theory]
    [MemberData(nameof(InvalidRequests))]
    public void InvalidFieldProducesValidationFailure(ReleaseAccountCreateRequest request)
    {
        // Arrange
        var validator = new ReleaseAccountCreateRequestValidator();

        // Act
        var result = validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
    }

    [Theory]
    [MemberData(nameof(ValidRequests))]
    public void ValidAndOmittedOptionalFieldsAreAccepted(ReleaseAccountCreateRequest request)
    {
        // Arrange
        var validator = new ReleaseAccountCreateRequestValidator();

        // Act
        var result = validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }
}
