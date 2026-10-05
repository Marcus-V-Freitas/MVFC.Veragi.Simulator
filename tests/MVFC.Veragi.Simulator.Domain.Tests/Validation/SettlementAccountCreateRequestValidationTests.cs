using FluentAssertions;
using Xunit;
using MVFC.Veragi.Simulator.Domain.Validation;
using MVFC.Veragi.Simulator.Shareable.Enums;
using MVFC.Veragi.Simulator.TestHelpers;
using MVFC.Veragi.Simulator.Shareable.Requests.Merchants;

namespace MVFC.Veragi.Simulator.Domain.Tests.Validation;

public sealed class SettlementAccountCreateRequestValidationTests
{
    public static TheoryData<SettlementAccountCreateRequest> InvalidRequests =>
    [
        MockEntities.FullSettlementAccountCreateRequest()with
        {
            CnpjRecipient = null
        },
        MockEntities.FullSettlementAccountCreateRequest()with
        {
            CnpjRecipient = "!invalid!"
        },
        MockEntities.FullSettlementAccountCreateRequest()with
        {
            CorporateNameRecipient = new string ('a', 101)
        },
        MockEntities.FullSettlementAccountCreateRequest()with
        {
            AccountType = null
        },
        MockEntities.FullSettlementAccountCreateRequest()with
        {
            AccountType = (AccountType)999
        },
        MockEntities.FullSettlementAccountCreateRequest()with
        {
            BankCode = null
        },
        MockEntities.FullSettlementAccountCreateRequest()with
        {
            BankCode = "!invalid!"
        },
        MockEntities.FullSettlementAccountCreateRequest()with
        {
            Ispb = null
        },
        MockEntities.FullSettlementAccountCreateRequest()with
        {
            Ispb = "!invalid!"
        },
        MockEntities.FullSettlementAccountCreateRequest()with
        {
            Branch = null
        },
        MockEntities.FullSettlementAccountCreateRequest()with
        {
            Branch = "!invalid!"
        },
        MockEntities.FullSettlementAccountCreateRequest()with
        {
            Account = null
        },
        MockEntities.FullSettlementAccountCreateRequest()with
        {
            Account = "!invalid!"
        },
    ];

    public static TheoryData<SettlementAccountCreateRequest> ValidRequests =>
    [
        MockEntities.FullSettlementAccountCreateRequest(),
        MockEntities.FullSettlementAccountCreateRequest()with
        {
            CorporateNameRecipient = null
        },
    ];

    [Theory]
    [MemberData(nameof(InvalidRequests))]
    public void InvalidFieldProducesValidationFailure(SettlementAccountCreateRequest request)
    {
        // Arrange
        var validator = new SettlementAccountCreateRequestValidator();

        // Act
        var result = validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
    }

    [Theory]
    [MemberData(nameof(ValidRequests))]
    public void ValidAndOmittedOptionalFieldsAreAccepted(SettlementAccountCreateRequest request)
    {
        // Arrange
        var validator = new SettlementAccountCreateRequestValidator();

        // Act
        var result = validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }
}
