using FluentAssertions;
using Xunit;
using MVFC.Veragi.Simulator.Domain.Validation;
using MVFC.Veragi.Simulator.Shareable.Enums;
using MVFC.Veragi.Simulator.TestHelpers;
using MVFC.Veragi.Simulator.Shareable.Requests.Merchants;

namespace MVFC.Veragi.Simulator.Domain.Tests.Validation;

public sealed class SettlementAccountPatchRequestValidationTests
{
    public static TheoryData<SettlementAccountPatchRequest> InvalidRequests =>
    [
        MockEntities.FullSettlementAccountPatchRequest()with
        {
            CnpjRecipient = null
        },
        MockEntities.FullSettlementAccountPatchRequest()with
        {
            CnpjRecipient = "!invalid!"
        },
        MockEntities.FullSettlementAccountPatchRequest()with
        {
            CorporateNameRecipient = new string ('a', 101)
        },
        MockEntities.FullSettlementAccountPatchRequest()with
        {
            AccountType = null
        },
        MockEntities.FullSettlementAccountPatchRequest()with
        {
            AccountType = (AccountType)999
        },
        MockEntities.FullSettlementAccountPatchRequest()with
        {
            BankCode = null
        },
        MockEntities.FullSettlementAccountPatchRequest()with
        {
            BankCode = "!invalid!"
        },
        MockEntities.FullSettlementAccountPatchRequest()with
        {
            Ispb = null
        },
        MockEntities.FullSettlementAccountPatchRequest()with
        {
            Ispb = "!invalid!"
        },
        MockEntities.FullSettlementAccountPatchRequest()with
        {
            Branch = null
        },
        MockEntities.FullSettlementAccountPatchRequest()with
        {
            Branch = "!invalid!"
        },
        MockEntities.FullSettlementAccountPatchRequest()with
        {
            Account = null
        },
        MockEntities.FullSettlementAccountPatchRequest()with
        {
            Account = "!invalid!"
        },
    ];

    public static TheoryData<SettlementAccountPatchRequest> ValidRequests =>
    [
        MockEntities.FullSettlementAccountPatchRequest(),
        MockEntities.FullSettlementAccountPatchRequest()with
        {
            CorporateNameRecipient = null
        },
    ];

    [Theory]
    [MemberData(nameof(InvalidRequests))]
    public void InvalidFieldProducesValidationFailure(SettlementAccountPatchRequest request)
    {
        // Arrange
        var validator = new SettlementAccountPatchRequestValidator();

        // Act
        var result = validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
    }

    [Theory]
    [MemberData(nameof(ValidRequests))]
    public void ValidAndOmittedOptionalFieldsAreAccepted(SettlementAccountPatchRequest request)
    {
        // Arrange
        var validator = new SettlementAccountPatchRequestValidator();

        // Act
        var result = validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }
}
