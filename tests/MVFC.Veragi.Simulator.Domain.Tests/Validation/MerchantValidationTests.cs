using FluentAssertions;
using Xunit;
using MVFC.Veragi.Simulator.Domain.Validation;
using MVFC.Veragi.Simulator.Shareable.Enums;
using MVFC.Veragi.Simulator.TestHelpers;
using MVFC.Veragi.Simulator.Shareable.Responses.Merchants;

namespace MVFC.Veragi.Simulator.Domain.Tests.Validation;

public sealed class MerchantValidationTests
{
    public static TheoryData<Merchant> InvalidRequests =>
    [
        MockEntities.FullMerchant()with
        {
            CorporateName = null
        },
        MockEntities.FullMerchant()with
        {
            CorporateName = ""
        },
        MockEntities.FullMerchant()with
        {
            CorporateName = new string ('a', 101)
        },
        MockEntities.FullMerchant()with
        {
            Cnpj = null
        },
        MockEntities.FullMerchant()with
        {
            Cnpj = "!invalid!"
        },
        MockEntities.FullMerchant()with
        {
            Email = new string ('a', 101)
        },
        MockEntities.FullMerchant()with
        {
            Email = "invalid"
        },
        MockEntities.FullMerchant()with
        {
            MobilePhone = "!invalid!"
        },
        MockEntities.FullMerchant()with
        {
            ReceivablesScheduleConfig = null
        },
        MockEntities.FullMerchant()with
        {
            CreditConfigurations = []
        },
        MockEntities.FullMerchant()with
        {
            CreditConfigurations = [CreditConfigurationType.CreditSecuredWorkingCapital, CreditConfigurationType.CreditSecuredWorkingCapital, CreditConfigurationType.CreditSecuredWorkingCapital, CreditConfigurationType.CreditSecuredWorkingCapital]
        },
        MockEntities.FullMerchant()with
        {
            LimitType = null
        },
        MockEntities.FullMerchant()with
        {
            LimitType = (LimitType)999
        },
        MockEntities.FullMerchant()with
        {
            LimitAmount = -1m
        },
        MockEntities.FullMerchant()with
        {
            ReleaseAccounts = [null !]
        },
    ];

    public static TheoryData<Merchant> ValidRequests =>
    [
        MockEntities.FullMerchant(),
        MockEntities.FullMerchant()with
        {
            Email = null
        },
        MockEntities.FullMerchant()with
        {
            MobilePhone = null
        },
        MockEntities.FullMerchant()with
        {
            CreditConfigurations = null
        },
        MockEntities.FullMerchant()with
        {
            LimitAmount = null
        },
        MockEntities.FullMerchant()with
        {
            ReleaseAccounts = null
        },
        MockEntities.FullMerchant()with
        {
            CreditSettlementAccount = null
        },
        MockEntities.FullMerchant()with
        {
            AnticipationSettlementAccount = null
        },
    ];

    [Theory]
    [MemberData(nameof(InvalidRequests))]
    public void InvalidFieldProducesValidationFailure(Merchant request)
    {
        // Arrange
        var validator = new MerchantValidator();

        // Act
        var result = validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
    }

    [Theory]
    [MemberData(nameof(ValidRequests))]
    public void ValidAndOmittedOptionalFieldsAreAccepted(Merchant request)
    {
        // Arrange
        var validator = new MerchantValidator();

        // Act
        var result = validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }
}
