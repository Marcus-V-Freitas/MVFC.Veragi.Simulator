using FluentAssertions;
using Xunit;
using MVFC.Veragi.Simulator.Domain.Validation;
using MVFC.Veragi.Simulator.Shareable.Enums;
using MVFC.Veragi.Simulator.TestHelpers;
using MVFC.Veragi.Simulator.Shareable.Requests.Merchants;

namespace MVFC.Veragi.Simulator.Domain.Tests.Validation;

public sealed class ReleaseAccountPatchRequestValidationTests
{
    public static TheoryData<ReleaseAccountPatchRequest> InvalidRequests =>
    [
        MockEntities.FullReleaseAccountPatchRequest()with
        {
            OperationType = null
        },
        MockEntities.FullReleaseAccountPatchRequest()with
        {
            OperationType = (OperationType)999
        },
        MockEntities.FullReleaseAccountPatchRequest()with
        {
            Id = "invalid"
        },
        MockEntities.FullReleaseAccountPatchRequest()with
        {
            ExternalId = new string ('a', 41)
        },
        MockEntities.FullReleaseAccountPatchRequest()with
        {
            AccountType = (ReleaseAccountType)999
        },
        MockEntities.FullReleaseAccountPatchRequest()with
        {
            BankCode = "!invalid!"
        },
        MockEntities.FullReleaseAccountPatchRequest()with
        {
            Ispb = "!invalid!"
        },
        MockEntities.FullReleaseAccountPatchRequest()with
        {
            Branch = "!invalid!"
        },
        MockEntities.FullReleaseAccountPatchRequest()with
        {
            Account = "!invalid!"
        },
    ];

    public static TheoryData<ReleaseAccountPatchRequest> ValidRequests => new()
    {
        MockEntities.FullReleaseAccountPatchRequest(),
        MockEntities.FullReleaseAccountPatchRequest()with
        {
            Id = null
        },
        MockEntities.FullReleaseAccountPatchRequest()with
        {
            ExternalId = null
        },
        MockEntities.FullReleaseAccountPatchRequest()with
        {
            AccountType = null
        },
        MockEntities.FullReleaseAccountPatchRequest()with
        {
            BankCode = null
        },
        MockEntities.FullReleaseAccountPatchRequest()with
        {
            Ispb = null
        },
        MockEntities.FullReleaseAccountPatchRequest()with
        {
            Branch = null
        },
        MockEntities.FullReleaseAccountPatchRequest()with
        {
            Account = null
        },
    };

    [Theory]
    [MemberData(nameof(InvalidRequests))]
    public void InvalidFieldProducesValidationFailure(ReleaseAccountPatchRequest request)
    {
        // Arrange
        var validator = new ReleaseAccountPatchRequestValidator();

        // Act
        var result = validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
    }

    [Theory]
    [MemberData(nameof(ValidRequests))]
    public void ValidAndOmittedOptionalFieldsAreAccepted(ReleaseAccountPatchRequest request)
    {
        // Arrange
        var validator = new ReleaseAccountPatchRequestValidator();

        // Act
        var result = validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }
}
