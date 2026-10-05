using FluentAssertions;
using Xunit;
using MVFC.Veragi.Simulator.Domain.Validation;
using MVFC.Veragi.Simulator.Shareable.Enums;
using MVFC.Veragi.Simulator.TestHelpers;
using MVFC.Veragi.Simulator.Shareable.Responses.Merchants;

namespace MVFC.Veragi.Simulator.Domain.Tests.Validation;

public sealed class SettlementAccountValidationTests
{
    public static TheoryData<SettlementAccount> InvalidRequests =>
    [
        MockEntities.FullSettlementAccount()with
        {
            CnpjRecipient = null
        },
        MockEntities.FullSettlementAccount()with
        {
            CnpjRecipient = "!invalid!"
        },
        MockEntities.FullSettlementAccount()with
        {
            CorporateNameRecipient = new string ('a', 101)
        },
        MockEntities.FullSettlementAccount()with
        {
            AccountType = null
        },
        MockEntities.FullSettlementAccount()with
        {
            AccountType = (AccountType)999
        },
        MockEntities.FullSettlementAccount()with
        {
            BankCode = null
        },
        MockEntities.FullSettlementAccount()with
        {
            BankCode = "!invalid!"
        },
        MockEntities.FullSettlementAccount()with
        {
            Ispb = null
        },
        MockEntities.FullSettlementAccount()with
        {
            Ispb = "!invalid!"
        },
        MockEntities.FullSettlementAccount()with
        {
            Branch = null
        },
        MockEntities.FullSettlementAccount()with
        {
            Branch = "!invalid!"
        },
        MockEntities.FullSettlementAccount()with
        {
            Account = null
        },
        MockEntities.FullSettlementAccount()with
        {
            Account = "!invalid!"
        },
    ];

    public static TheoryData<SettlementAccount> ValidRequests =>
    [
        MockEntities.FullSettlementAccount(),
        MockEntities.FullSettlementAccount()with
        {
            CorporateNameRecipient = null
        },
    ];

    [Theory]
    [MemberData(nameof(InvalidRequests))]
    public void InvalidFieldProducesValidationFailure(SettlementAccount request)
    {
        // Arrange
        var validator = new SettlementAccountValidator();

        // Act
        var result = validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
    }

    [Theory]
    [MemberData(nameof(ValidRequests))]
    public void ValidAndOmittedOptionalFieldsAreAccepted(SettlementAccount request)
    {
        // Arrange
        var validator = new SettlementAccountValidator();

        // Act
        var result = validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }
}
